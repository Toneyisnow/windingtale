

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


        private static GameInterface instance = new GameInterface();


        public void OnMapClicked(FDPosition position)
        {
            Debug.Log("Clicked at : " + position.X + " " + position.Y);

            GameObject cursor = MapIndicators.transform.Find("Cursor")?.gameObject;
            if (cursor == null)
            {
                cursor = Instantiate(Resources.Load<GameObject>("Others/Cursor"));
                cursor.name = "Cursor";
                cursor.transform.SetParent(MapIndicators.transform);
            }

            cursor.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(position), Quaternion.identity);

        }

    }


}