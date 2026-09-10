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

        public static ChapterEvents CreateChapter(GameMain gameMain, int chapterId)
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
                case 3:
                    chapter = new Chapter3(gameMain);
                    break;
                case 4:
                    chapter = new Chapter4(gameMain);
                    break;
                case 5:
                    chapter = new Chapter5(gameMain);
                    break;
                case 6:
                    chapter = new Chapter6(gameMain);
                    break;
                case 7:
                    chapter = new Chapter7(gameMain);
                    break;
                case 8:
                    chapter = new Chapter8(gameMain);
                    break;
                case 9:
                    chapter = new Chapter9(gameMain);
                    break;
                case 10:
                    chapter = new Chapter10(gameMain);
                    break;
                case 11:
                    chapter = new Chapter11(gameMain);
                    break;
                case 12:
                    chapter = new Chapter12(gameMain);
                    break;
                case 13:
                    chapter = new Chapter13(gameMain);
                    break;
                case 14:
                    chapter = new Chapter14(gameMain);
                    break;
                case 15:
                    chapter = new Chapter15(gameMain);
                    break;
                case 16:
                    chapter = new Chapter16(gameMain);
                    break;
                case 17:
                    chapter = new Chapter17(gameMain);
                    break;
                case 18:
                    chapter = new Chapter18(gameMain);
                    break;
                case 19:
                    chapter = new Chapter19(gameMain);
                    break;
                case 20:
                    chapter = new Chapter20(gameMain);
                    break;
                case 21:
                    chapter = new Chapter21(gameMain);
                    break;
                default:
                    break;
            }

            if (chapter == null)
            {
                throw new Exception("Cannot find definition for chapter " + chapterId);
            }

            return chapter;
        }


        public static List<FDEvent> LoadEvents(GameMain gameMain, int chapterId)
        {
            ChapterEvents chapter = CreateChapter(gameMain, chapterId);

            return chapter.AllEvents;
        }

        public static void AdjustFriendsAfterWon(GameMain gameMain, int chapterId)
        {
            ChapterEvents chapter = CreateChapter(gameMain, chapterId);
            chapter.AdjustFriendsAfterWon();
        }
        
    }
}