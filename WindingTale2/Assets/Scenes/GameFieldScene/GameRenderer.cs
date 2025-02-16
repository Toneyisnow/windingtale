using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene
{

    public class GameRenderer : MonoBehaviour
    {
        public Material DefaultMaterial;

        public Material DefaultGreyMaterial;

        private static GameRenderer instance = null;


        public static GameRenderer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameRenderer>(); //// GameObject.Find("GameRoot").GetComponent<GameRenderer>();
                    if (instance == null)
                    {
                        throw new MissingComponentException("Cannot find component GameRenderer");
                    }
                }
                return instance;
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