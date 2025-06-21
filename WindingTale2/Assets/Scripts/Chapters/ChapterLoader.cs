using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Events;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    public class ChapterLoader
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        //public static ChapterDefinition LoadChapter(int chapterId)
        //{
        //    // Load Chapter
        //    ChapterDefinition definition = ResourceJsonFile.Load<ChapterDefinition>(string.Format(@"Data/Chapters/Chapter_{0}", StringUtils.Digit2(chapterId)));
        //    if (definition == null)
        //    {
        //        throw new Exception("Cannot find definition for chapter " + chapterId);
        //    }
            
        //    definition.ChapterId = chapterId;

        //    return definition;
        //}

        public static List<FDEvent> LoadEvents(GameMain gameMain, int chapterId)
        {
            ChapterEvents chapter = null;
            switch (chapterId)
            {
                case 1:
                    chapter = new Chapter1(gameMain);
                    break;
                case 2:
                    chapter = new Chapter2(gameMain);
                    break;
                default:
                    break;
            }

            if (chapter == null)
            {
                throw new Exception("Cannot find definition for chapter " + chapterId);
            }

            return chapter.AllEvents;
        }
        
    }
}