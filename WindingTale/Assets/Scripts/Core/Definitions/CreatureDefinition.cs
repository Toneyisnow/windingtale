using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public enum CreatureFaction
    {
        Friend = 0,
        Enemy = 1,
        Npc = 2,
    }

    /// <summary>
    /// 
    /// </summary>
    public class CreatureDefinition
    {
        public int DefinitionId
        {
            get; set;
        }

        public int AnimationId
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }

        public int Race
        {
            get; set;
        }

        public int Occupation
        {
            get; set;
        }

        public int InitialLevel
        {
            get; set;
        }

        public int InitialHp
        {
            get; set;
        }

        public int InitialMp
        {
            get; set;
        }

        public int InitialAp
        {
            get; set;
        }

        public int InitialDp
        {
            get; set;
        }

        public int InitialDx
        {
            get; set;
        }

        public int InitialMv
        {
            get; set;
        }

        public int InitialEx
        {
            get; set;
        }



    }
}
