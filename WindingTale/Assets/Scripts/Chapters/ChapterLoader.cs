using WindingTale.Common;
using WindingTale.Core.Definitions;

namespace WindingTale.Chapters
{
    public class ChapterLoader
    {
        public ChapterLoader()
        {
        }
        public static ChapterDefinition LoadChapter(int chapterId)
        {
            // Load Chapter
            ChapterDefinition definition = ResourceJsonFile.Load<ChapterDefinition>(string.Format(@"Data/Chapters/Chapter_{0}", StringUtils.Digit2(chapterId)));
            definition.ChapterId = chapterId;

            ChapterDefinition chapter = null;
            switch (chapterId)
            {
                case 1:
                    chapter = new Chapter1();
                    break;
                case 2:
                    chapter = new Chapter2();
                    break;
                case 3:
                    chapter = new Chapter3();
                    break;
                default:
                    break;
            }

            if (chapter == null)
            {
                throw new System.Exception("Cannot find definition for chapter " + chapterId);
            }

            chapter.LoadEvents();
            return chapter;
        }
    }
}