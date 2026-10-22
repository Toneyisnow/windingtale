using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.UI.Audio;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.GameFreeFieldScene
{
    /// <summary>
    /// A map the hero walks about freely -- no turns, no battle. It keeps three things of
    /// GameFieldScene: the board (ShapesLayer, with the props of ObstaclesLayer standing on
    /// it), the creatures standing on it (the same 3D CreatureIcon models), and the talk
    /// dialog at the top or bottom of the screen with its typing and scrolling.
    ///
    /// The arrow keys walk the hero (creature definition 001) one tile at a time, turned the
    /// way he goes, with GameFieldScene's footsteps; holding a key keeps him walking. He
    /// cannot walk onto a Blocked tile (ShapeMatrix), off the board, or into another
    /// creature -- he turns to face it instead. The map's Events start talks: a Step event
    /// when he arrives on one of its tiles, a Near event when he arrives next to one, a
    /// Face event when he comes to face one (Space / Enter plays a Face event again). The
    /// lines play one after another in the TalkDialog (FreeFieldTalkQueue); when they are
    /// over, the event's Then may put creatures on the map, move on to another map, or
    /// start a battle chapter. Holding Space / Enter on the skip bar in the bottom-right
    /// corner (FreeFieldSkipBar) jumps straight to wherever the map leads.
    ///
    /// Everything comes from Resources/Data/Maps/Map_NN.json (FreeFieldDefinition) and the
    /// lines from the ChapterStrings-NN table, NN being <see cref="mapId"/>.
    /// </summary>
    public class GameFreeFieldScene : MonoBehaviour
    {
        /// <summary>This scene's name in the build settings.</summary>
        public const string SceneName = "GameFreeFieldScene";

        /// <summary>The GlobalVariables key another scene hands the map id over on.</summary>
        public const string MapIdVariableName = "FreeFieldMapId";

        /// <summary>The map to open when nothing was handed over (the scene opened on its own).</summary>
        public int mapId = 32;

        /// <summary>The field's creature model, MapObjects/CreatureIcon/CreatureIcon.prefab.</summary>
        public GameObject creatureIconPrefab = null;

        /// <summary>The scene's TalkDialog (UI/Dialogs/TalkDialog.prefab under the canvas).</summary>
        public GameObject talkDialog = null;

        // Where GameFieldScene's GameMap puts its ShapesLayer: 1.6 up, with the creatures at
        // y = 2 (MapCoordinate.ConvertCreaturePosToVec3) and the obstacles seated at y = 0.
        // The tiles are raised rather than the rest lowered, so creatures and props stand on
        // the board exactly as they do in battle.
        private const float ShapesLayerHeight = 1.6f;

        // The camera looks down on the hero from the south, like the field camera's default.
        private const float CameraPitch = 50f;
        private const float CameraYaw = 180f;
        private const float CameraDistance = 18f;
        private const float CameraSmoothTime = 0.25f;

        private FreeFieldDefinition definition = null;
        private FDMap map = null;
        private FreeFieldTalkQueue talkQueue = null;

        private GameObject creaturesLayer = null;

        private FDCreature hero = null;
        private FreeFieldWalker heroWalker = null;

        // The tile the hero faced when an event last had its chance to fire, so standing
        // in front of something does not start the talk over and over.
        private FDPosition lastFacedTile = null;

        // Set when the hero starts a step; the Step events of the tile are looked at once he
        // has arrived, before the next step can start.
        private bool arrivalPending = false;

        // What the last event asked for once its talk is over (FreeFieldThenDefinition).
        private FreeFieldThenDefinition pendingThen = null;

        // The Once events (by their index in definition.Events) that have played.
        private readonly HashSet<int> playedEvents = new HashSet<int>();

        // Every creature model on the board by creature Id, for the events' actions.
        private readonly Dictionary<int, GameObject> iconsById = new Dictionary<int, GameObject>();

        // The direction key pressed last; it wins while several are held.
        private FreeFieldFacing? heldDirection = null;

        private Vector3 cameraVelocity = Vector3.zero;

        private bool ready = false;

        IEnumerator Start()
        {
            if (GlobalVariables.Get<object>(MapIdVariableName) is int handedOver)
            {
                mapId = handedOver;
            }

            definition = ResourceJsonFile.Load<FreeFieldDefinition>(
                string.Format("Data/Maps/Map_{0}", StringUtils.Digit2(mapId)));
            if (definition == null)
            {
                Debug.LogError(string.Format("GameFreeFieldScene: Data/Maps/Map_{0} not found.", StringUtils.Digit2(mapId)));
                yield break;
            }
            definition.ChapterId = mapId;
            map = FDMap.LoadFromDefinition(definition);

            GameObject shapesObject = CreateLayer("ShapesLayer");
            shapesObject.transform.localPosition = new Vector3(0f, ShapesLayerHeight, 0f);
            ShapesLayer shapesLayer = shapesObject.AddComponent<ShapesLayer>();
            ObstaclesLayer obstaclesLayer = CreateLayer("ObstaclesLayer").AddComponent<ObstaclesLayer>();
            creaturesLayer = CreateLayer("CreaturesLayer");

            if (talkDialog != null)
            {
                talkDialog.SetActive(false);
            }
            talkQueue = new FreeFieldTalkQueue(talkDialog, definition.StringsTable > 0 ? definition.StringsTable : mapId);

            // ShapesLayer picks up the tile material in its Start; build after it has run.
            yield return null;

            shapesLayer.Initialize(map.Field);
            obstaclesLayer.Initialize(map, null);

            if (definition.Creatures != null)
            {
                foreach (FreeFieldCreatureDefinition c in definition.Creatures)
                {
                    PlaceCreature(c);
                }
            }

            FreeFieldHeroDefinition heroDefinition = definition.Hero ?? new FreeFieldHeroDefinition() { DefinitionId = 1, X = 1, Y = 1 };
            CreatureDefinition heroCreature = DefinitionStore.Instance.GetCreatureDefinition(heroDefinition.DefinitionId)
                ?? new CreatureDefinition() { DefinitionId = heroDefinition.DefinitionId, AnimationId = heroDefinition.DefinitionId };
            hero = new FDCreature(heroDefinition.DefinitionId, heroCreature, CreatureFaction.Friend);
            GameObject heroIcon = AddCreature(hero, FDPosition.At(heroDefinition.X, heroDefinition.Y), ParseFacing(heroDefinition.Facing));
            heroWalker = heroIcon.GetComponent<FreeFieldWalker>();

            SetUpCamera();

            // Whatever he faces on arrival does not count as coming to face it.
            lastFacedTile = FacedTile();
            ready = true;

            // The map's hint ("按方向键操作角色" on Map 32), until the first arrow key.
            if (!string.IsNullOrEmpty(definition.Hint) && talkDialog != null && talkDialog.transform.parent != null)
            {
                // Shown once the map has faded in from black (SceneTransition's fade-in, which
                // started with the scene, a frame before this).
                SceneTransition transition = FindFirstObjectByType<SceneTransition>();
                float fadeIn = transition != null ? transition.fadeDuration : 0f;
                FreeFieldHintBar.Create(talkDialog.transform.parent, definition.Hint, fadeIn);
            }

            // The "按空格跳过" bar, wherever the map leads on to somewhere.
            if (NextScene() != null && talkDialog != null && talkDialog.transform.parent != null)
            {
                FreeFieldSkipBar.Create(talkDialog.transform.parent, Skip);
            }
        }

        /// <summary>
        /// Where the map leads on to: the last event that moves to another map or starts a
        /// battle chapter. Null for a map that leads nowhere.
        /// </summary>
        private FreeFieldThenDefinition NextScene()
        {
            if (definition == null || definition.Events == null)
            {
                return null;
            }

            FreeFieldEventDefinition last = definition.Events.FindLast(e => e.Then != null && (e.Then.Map > 0 || e.Then.Chapter > 0));
            return last != null ? last.Then : null;
        }

        /// <summary>The skip bar has filled: leave for the next scene at once, talk and all.</summary>
        private void Skip()
        {
            FreeFieldThenDefinition next = NextScene();
            if (!ready || next == null)
            {
                return;
            }

            // Only the way on -- creatures it would have put on this map do not matter now.
            RunThen(new FreeFieldThenDefinition { Map = next.Map, Chapter = next.Chapter });
        }

        void Update()
        {
            if (!ready)
            {
                return;
            }

            talkQueue.Update();

            TrackDirectionKeys();

            if (talkQueue.IsBusy || talkQueue.WasClosedThisFrame())
            {
                return;
            }

            if (pendingThen != null)
            {
                FreeFieldThenDefinition then = pendingThen;
                pendingThen = null;
                RunThen(then);
                if (!ready)
                {
                    return;
                }
            }

            if (heroWalker.IsWalking)
            {
                return;
            }

            if (arrivalPending)
            {
                arrivalPending = false;
                if (PlayEvent(hero.Position, FreeFieldTrigger.Step) || PlayEvent(hero.Position, FreeFieldTrigger.Near))
                {
                    heroWalker.Stop();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                heroWalker.Stop();
                PlayEvent(FacedTile(), FreeFieldTrigger.Face);
                return;
            }

            if (heldDirection.HasValue)
            {
                TryStep(heldDirection.Value);
            }
            else
            {
                heroWalker.Stop();
            }

            CheckFacedEvent();
        }

        void LateUpdate()
        {
            if (!ready)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.transform.position = Vector3.SmoothDamp(camera.transform.position, CameraPositionFor(heroWalker.transform.position),
                ref cameraVelocity, CameraSmoothTime);
        }

        #region Hero

        private void TrackDirectionKeys()
        {
            // The newest press wins; when it is let go, any other key still held takes over.
            if (Input.GetKeyDown(KeyCode.UpArrow)) heldDirection = FreeFieldFacing.Up;
            if (Input.GetKeyDown(KeyCode.DownArrow)) heldDirection = FreeFieldFacing.Down;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) heldDirection = FreeFieldFacing.Left;
            if (Input.GetKeyDown(KeyCode.RightArrow)) heldDirection = FreeFieldFacing.Right;

            if (heldDirection.HasValue && !Input.GetKey(KeyOf(heldDirection.Value)))
            {
                heldDirection = null;
                foreach (FreeFieldFacing facing in new[] { FreeFieldFacing.Up, FreeFieldFacing.Down, FreeFieldFacing.Left, FreeFieldFacing.Right })
                {
                    if (Input.GetKey(KeyOf(facing)))
                    {
                        heldDirection = facing;
                        break;
                    }
                }
            }
        }

        /// <summary>Turns the hero that way and walks him a tile on when the tile is free.</summary>
        private void TryStep(FreeFieldFacing facing)
        {
            FDPosition target = Neighbour(hero.Position, facing);
            if (!CanWalkOnto(target))
            {
                heroWalker.Stop();
                heroWalker.Face(facing);
                return;
            }

            hero.Position = target;
            arrivalPending = true;
            heroWalker.StepTo(target, facing, SoundEffects.GetWalkSurface(hero.Definition, map.Field.GetShapeAt(target)));
        }

        private bool CanWalkOnto(FDPosition position)
        {
            ShapeDefinition shape = map.Field.GetShapeAt(position);
            if (shape == null || shape.MoveCost < 0)
            {
                return false;
            }

            return map.Creatures.Find(c => c != hero && c.Position != null && c.Position.AreSame(position)) == null;
        }

        private FDPosition FacedTile()
        {
            return Neighbour(hero.Position, heroWalker.Facing);
        }

        /// <summary>Plays the event of the tile the hero faces, once each time he comes to face it.</summary>
        private void CheckFacedEvent()
        {
            if (heroWalker.IsWalking)
            {
                return;
            }

            FDPosition faced = FacedTile();
            if (lastFacedTile != null && lastFacedTile.AreSame(faced))
            {
                return;
            }

            lastFacedTile = faced;
            if (PlayEvent(faced, FreeFieldTrigger.Face))
            {
                heroWalker.Stop();
            }
        }

        /// <summary>
        /// Plays the first event of the given kind that fires at the tile -- for Face and
        /// Step the tile is one of its Positions, for Near the hero stands next to one -- and
        /// may still play: a Once event only the first time, one with Requires only after
        /// those. True when one did.
        /// </summary>
        private bool PlayEvent(FDPosition position, FreeFieldTrigger trigger)
        {
            if (definition.Events == null)
            {
                return false;
            }

            int index = -1;
            for (int i = 0; i < definition.Events.Count && index < 0; i++)
            {
                FreeFieldEventDefinition e = definition.Events[i];
                bool fires = trigger == FreeFieldTrigger.Near ? e.IsNextTo(position) : e.Covers(position);
                if (e.TriggerKind == trigger && e.Talks != null && fires
                    && !(e.Once && playedEvents.Contains(i)) && RequirementsMet(e))
                {
                    index = i;
                }
            }

            if (index < 0)
            {
                return false;
            }

            FreeFieldEventDefinition found = definition.Events[index];
            playedEvents.Add(index);
            pendingThen = found.Then;

            foreach (FreeFieldTalkDefinition talk in found.Talks)
            {
                for (int sequence = talk.From; sequence <= talk.To; sequence++)
                {
                    int line = sequence - talk.From;
                    int speaker = talk.Speakers != null && line < talk.Speakers.Count ? talk.Speakers[line] : 0;

                    // As in the battle: the party talks from the bottom, everyone else (and the
                    // narrator) from the top.
                    FDCreature speaking = map.Creatures.Find(c => c.Definition != null && c.Definition.AnimationId == speaker);
                    GameCanvas.DialogPosition dialogPosition = speaker > 0 && speaking != null && speaking.Faction == CreatureFaction.Friend
                        ? GameCanvas.DialogPosition.Bottom
                        : GameCanvas.DialogPosition.Top;
                    PushActions(found, talk.ConversationId, sequence, before: true);
                    talkQueue.Push(talk.ConversationId, sequence, speaker, dialogPosition);
                    PushActions(found, talk.ConversationId, sequence, before: false);
                }
            }

            return true;
        }

        /// <summary>Queues the event's actions that go just before (or just after) one line.</summary>
        private void PushActions(FreeFieldEventDefinition e, int conversationId, int sequenceId, bool before)
        {
            if (e.Actions == null)
            {
                return;
            }

            foreach (FreeFieldActionDefinition action in e.Actions)
            {
                if (before ? !action.IsBefore(conversationId, sequenceId) : !action.IsAfter(conversationId, sequenceId))
                {
                    continue;
                }

                if (action.Walk != null)
                {
                    FreeFieldWalkDefinition walk = action.Walk;
                    List<FDPosition> path = null;
                    talkQueue.PushAction(
                        () => { path = StartWalk(walk.Creature, FDPosition.At(walk.X, walk.Y)); },
                        () => ContinueWalk(walk.Creature, path));
                }

                if (action.Stand > 0)
                {
                    int creatureId = action.Stand;
                    talkQueue.PushAction(() =>
                    {
                        FreeFieldPose pose = iconsById.TryGetValue(creatureId, out GameObject icon) ? icon.GetComponent<FreeFieldPose>() : null;
                        if (pose != null)
                        {
                            pose.StandUp();
                        }
                    }, () => true);
                }
            }
        }

        /// <summary>
        /// The tiles a creature walks to reach <paramref name="target"/>, round the blocked
        /// tiles and the other creatures (a breadth-first search over the board), first step
        /// first; empty when it is there already. A target it cannot reach is jumped to.
        /// </summary>
        private List<FDPosition> StartWalk(int creatureId, FDPosition target)
        {
            List<FDPosition> path = new List<FDPosition>();
            FDCreature creature = map.Creatures.Find(c => c.Id == creatureId);
            if (creature == null || !iconsById.ContainsKey(creatureId) || creature.Position.AreSame(target))
            {
                return path;
            }

            Dictionary<int, FDPosition> cameFrom = new Dictionary<int, FDPosition>();
            Queue<FDPosition> open = new Queue<FDPosition>();
            open.Enqueue(creature.Position);
            cameFrom[TileKey(creature.Position)] = null;
            bool found = false;
            while (open.Count > 0 && !found)
            {
                FDPosition at = open.Dequeue();
                foreach (FreeFieldFacing facing in new[] { FreeFieldFacing.Up, FreeFieldFacing.Down, FreeFieldFacing.Left, FreeFieldFacing.Right })
                {
                    FDPosition next = Neighbour(at, facing);
                    if (cameFrom.ContainsKey(TileKey(next)) || !IsFreeFor(creature, next))
                    {
                        continue;
                    }

                    cameFrom[TileKey(next)] = at;
                    if (next.AreSame(target))
                    {
                        found = true;
                        break;
                    }
                    open.Enqueue(next);
                }
            }

            if (!found)
            {
                // Nowhere to walk through: put it there.
                creature.Position = target;
                iconsById[creatureId].transform.position = MapCoordinate.ConvertCreaturePosToVec3(target);
                return path;
            }

            for (FDPosition at = target; at != null && !at.AreSame(creature.Position); at = cameFrom[TileKey(at)])
            {
                path.Insert(0, at);
            }
            return path;
        }

        /// <summary>Takes the walk's next step whenever the last one is done; true once it has arrived.</summary>
        private bool ContinueWalk(int creatureId, List<FDPosition> path)
        {
            if (path == null || !iconsById.TryGetValue(creatureId, out GameObject icon))
            {
                return true;
            }

            FreeFieldWalker walker = icon.GetComponent<FreeFieldWalker>();
            if (walker.IsWalking)
            {
                return false;
            }

            FDCreature creature = map.Creatures.Find(c => c.Id == creatureId);
            if (path.Count == 0 || creature == null)
            {
                walker.Stop();
                return true;
            }

            FDPosition next = path[0];
            path.RemoveAt(0);
            creature.Position = next;
            walker.StepTo(next, FacingTowards(walker.transform.position, next),
                SoundEffects.GetWalkSurface(creature.Definition, map.Field.GetShapeAt(next)));
            return false;
        }

        private bool IsFreeFor(FDCreature walker, FDPosition position)
        {
            ShapeDefinition shape = map.Field.GetShapeAt(position);
            if (shape == null || shape.MoveCost < 0)
            {
                return false;
            }

            return map.Creatures.Find(c => c != walker && c.Position != null && c.Position.AreSame(position)) == null;
        }

        private static FreeFieldFacing FacingTowards(Vector3 from, FDPosition to)
        {
            Vector3 delta = MapCoordinate.ConvertCreaturePosToVec3(to) - from;
            // Map X runs along world -X and map Y along world +Z (MapCoordinate).
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
            {
                return delta.x < 0 ? FreeFieldFacing.Right : FreeFieldFacing.Left;
            }
            return delta.z > 0 ? FreeFieldFacing.Down : FreeFieldFacing.Up;
        }

        private static int TileKey(FDPosition position)
        {
            return position.X * 1000 + position.Y;
        }

        private bool RequirementsMet(FreeFieldEventDefinition e)
        {
            if (e.Requires == null)
            {
                return true;
            }

            foreach (int required in e.Requires)
            {
                int index = definition.Events.FindIndex(other => other.Id == required);
                if (index < 0 || !playedEvents.Contains(index))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// What an event does once its talk is over: puts its Spawn creatures on the map,
        /// then, if it names a Chapter, fades out to that battle in GameFieldScene, or if it
        /// names a Map, fades out and opens that map in this scene.
        /// </summary>
        private void RunThen(FreeFieldThenDefinition then)
        {
            if (then.Spawn != null)
            {
                foreach (FreeFieldCreatureDefinition c in then.Spawn)
                {
                    PlaceCreature(c);
                }
            }

            if (then.Chapter > 0)
            {
                // The battle chapter, begun as the title's New Game used to begin chapter 1.
                GlobalVariables.Set(GameFiledSceneParams.ChapterIdVariableName, then.Chapter);
                GameFiledSceneParams.isContinue = false;
                LeaveFor("GameFieldScene");
            }
            else if (then.Map > 0)
            {
                GlobalVariables.Set(MapIdVariableName, then.Map);
                LeaveFor(SceneName);
            }
        }

        /// <summary>Stops the hero and fades out to the scene (this one again for another map).</summary>
        private void LeaveFor(string sceneName)
        {
            ready = false;
            heroWalker.Stop();

            SceneTransition transition = FindFirstObjectByType<SceneTransition>();
            if (transition != null)
            {
                transition.LoadScene(sceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        #endregion

        #region Building

        /// <summary>
        /// Puts one of the map's creatures on the board: a party member by its creature
        /// definition (DefinitionId), or an extra drawn only with an AnimationId's icon.
        /// </summary>
        private void PlaceCreature(FreeFieldCreatureDefinition c)
        {
            CreatureDefinition creatureDefinition = c.DefinitionId > 0
                ? DefinitionStore.Instance.GetCreatureDefinition(c.DefinitionId)
                : null;
            CreatureFaction faction = creatureDefinition != null ? CreatureFaction.Friend : CreatureFaction.Npc;
            if (creatureDefinition == null)
            {
                int animationId = c.AnimationId > 0 ? c.AnimationId : c.DefinitionId;
                creatureDefinition = new CreatureDefinition() { DefinitionId = c.Id, AnimationId = animationId };
            }

            FDCreature creature = new FDCreature(c.Id, creatureDefinition, faction);
            GameObject icon = AddCreature(creature, FDPosition.At(c.X, c.Y), ParseFacing(c.Facing));

            if (string.Equals(c.Pose, "Lying", System.StringComparison.OrdinalIgnoreCase))
            {
                icon.AddComponent<FreeFieldPose>().LieDown();
            }
        }

        private GameObject CreateLayer(string name)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(this.transform, false);
            return layer;
        }

        /// <summary>Puts a creature model on the board, as GameMap.AddCreatureUI does.</summary>
        private GameObject AddCreature(FDCreature creature, FDPosition position, FreeFieldFacing facing)
        {
            creature.Position = position;
            map.Creatures.Add(creature);

            GameObject icon = Instantiate(creatureIconPrefab);
            icon.name = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            icon.transform.SetParent(creaturesLayer.transform);
            icon.transform.SetPositionAndRotation(MapCoordinate.ConvertCreaturePosToVec3(position), FreeFieldWalker.RotationFor(facing));

            string id = StringUtils.Digit3(creature.Definition.AnimationId);
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_01", id), icon.transform.Find("Clip_01"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_02", id), icon.transform.Find("Clip_02"));
            AttachIcon(string.Format("Icons/{0}/Icon_{0}_03", id), icon.transform.Find("Clip_03"));

            Creature component = icon.GetComponent<Creature>();
            component.SetCreature(creature);
            component.InitializeClipVisibility();

            FreeFieldWalker walker = icon.AddComponent<FreeFieldWalker>();
            walker.Face(facing);
            iconsById[creature.Id] = icon;
            return icon;
        }

        private static void AttachIcon(string iconFilePath, Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(iconFilePath);
            if (prefab == null || parent == null)
            {
                Debug.LogWarning("GameFreeFieldScene: creature icon not found: " + iconFilePath);
                return;
            }

            GameObject icon = Instantiate(prefab);
            CreatureMaterial.Apply(icon);
            icon.transform.SetParent(parent);
            icon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void SetUpCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            // A map that names a sky is outdoors and gets a plain sky blue past its edges;
            // indoors (the throne hall) there is just a dark backdrop past the walls.
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = string.IsNullOrEmpty(definition.Sky)
                ? new Color(0.06f, 0.06f, 0.08f)
                : new Color(0.45f, 0.65f, 0.9f);
            camera.transform.rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            camera.transform.position = CameraPositionFor(heroWalker.transform.position);
        }

        private static Vector3 CameraPositionFor(Vector3 target)
        {
            Quaternion rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            return target - rotation * Vector3.forward * CameraDistance;
        }

        #endregion

        #region Helpers

        private static FDPosition Neighbour(FDPosition position, FreeFieldFacing facing)
        {
            switch (facing)
            {
                case FreeFieldFacing.Up: return FDPosition.At(position.X, position.Y - 1);
                case FreeFieldFacing.Left: return FDPosition.At(position.X - 1, position.Y);
                case FreeFieldFacing.Right: return FDPosition.At(position.X + 1, position.Y);
                default: return FDPosition.At(position.X, position.Y + 1);
            }
        }

        private static KeyCode KeyOf(FreeFieldFacing facing)
        {
            switch (facing)
            {
                case FreeFieldFacing.Up: return KeyCode.UpArrow;
                case FreeFieldFacing.Left: return KeyCode.LeftArrow;
                case FreeFieldFacing.Right: return KeyCode.RightArrow;
                default: return KeyCode.DownArrow;
            }
        }

        private static FreeFieldFacing ParseFacing(string facing)
        {
            if (!string.IsNullOrEmpty(facing) && System.Enum.TryParse(facing, true, out FreeFieldFacing parsed))
            {
                return parsed;
            }

            return FreeFieldFacing.Down;
        }

        #endregion
    }
}
