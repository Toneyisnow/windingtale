using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.Legacy.Core.Components.Data
{
    public class ChapterRecord
    {
        public int ChapterId
        {
            get; set;
        }

        private List<CreatureData> creatureDataList = null;

        public static ChapterRecord NewGame()
        {
            ChapterRecord record = new ChapterRecord();

            record.ChapterId = 1;
            record.creatureDataList = new List<CreatureData>(); // Empty data

            return record;
        }
    }
}