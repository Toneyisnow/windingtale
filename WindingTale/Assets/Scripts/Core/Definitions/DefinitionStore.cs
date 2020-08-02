using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Load two files: chapter_N.dat for json, chapter_N_data.dat for plain text
        /// </summary>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        public ChapterDefinition LoadChapter(int chapterId)
        {
            string mapData = File.ReadAllText(string.Format(@"D:\GitRoot\toneyisnow\windingtale\WindingTale\Assets\Resources\Data\Chapters\Chapter_{0}.dat", chapterId));
            ChapterDefinition chapter = JsonConvert.DeserializeObject<ChapterDefinition>(mapData);
            chapter.ChapterId = chapterId;

            return chapter;
        }
    }
}