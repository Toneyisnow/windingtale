

using Unity;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Common;

namespace WindingTale.UI.Scenes.Game
{
    public interface IGameInterface
    {
        /////public UICreature GetUICreature(int creatureId);
        ///

        public void AddCreatureUI(FDCreature creature, FDPosition position);

        public void MoveCreatureUI(FDCreature creature, FDMovePath path);
    }

    public class GameInterface : MonoBehaviour
    {
        public GameObject MapObjects;

        public GameObject MapField;

        public GameObject MapIndicators;

        public Material DefaultMaterial;

        public Material DefaultGreyMaterial;

        private static GameInterface instance = null;


        void Awake()
        {
            instance = this;
        }


        public static GameInterface Instance
        {
            get
            {
                return instance;
            }
        }

        public void OnMapClicked(FDPosition position)
        {
            // Update cursor
            GameObject cursor = MapIndicators.transform.Find("Cursor")?.gameObject;
            if (cursor == null)
            {
                cursor = Instantiate(Resources.Load<GameObject>("Others/Cursor/CursorPrefab"));
                cursor.name = "Cursor";

                Cursor c = cursor.AddComponent<Cursor>();
                c.Position = position;

                cursor.transform.SetParent(MapIndicators.transform);
                cursor.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);
            }
            else if (!cursor.GetComponent<Cursor>().Position.AreSame(position))
            {
                cursor.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);
                cursor.GetComponent<Cursor>().Position = position;
            }
            else
            {
                // Take action
                GameMain.Instance.State.OnSelectPosition(position);
            }
        }


        public void ApplyDefaultMaterial(GameObject gameObject)
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }

            renderer.materials = new Material[1] { DefaultMaterial };
        }

        public void ApplyDefaultGreyMaterial(GameObject gameObject)
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            renderer.materials = new Material[1] { DefaultGreyMaterial };
        }
    }


}