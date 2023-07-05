

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


        /// <summary>
        /// /private static GameInterface instance = new GameInterface();
        /// </summary>
        /// <param name="position"></param>


        public void OnMapClicked(FDPosition position)
        {
            Debug.Log("Clicked at : " + position.X + " " + position.Y);

            // Update cursor
            GameObject cursor = MapIndicators.transform.Find("Cursor")?.gameObject;
            if (cursor == null)
            {
                cursor = Instantiate(Resources.Load<GameObject>("Others/Cursor"));
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
    }


}