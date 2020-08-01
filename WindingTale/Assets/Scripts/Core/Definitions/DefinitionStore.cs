using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Core.Definitions
{


    public class DefinitionStore
    {
        private static DefinitionStore instance = null;

        private DefinitionStore()
        {

        }

        public static DefinitionStore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DefinitionStore();
                    instance.LoadAll();
                }
                return instance;
            }
        }

        private void LoadAll()
        {

        }

        private void LoadCreatureDefinitions()
        {

        }

        private void LoadItemDefinitions()
        {

        }


    }
}