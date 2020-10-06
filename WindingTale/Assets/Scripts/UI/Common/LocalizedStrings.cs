using SmartLocalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Common;

namespace Assets.Scripts.UI.Common
{
    public class LocalizedStrings
    {
        public static void SetLanguage(string lang)
        {
            LanguageManager.Instance.ChangeLanguage(lang);

        }


        public static string GetCreatureName(int animationId)
        {
            string key = string.Format(@"CREATURE_{0}", StringUtils.Digit3(animationId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetItemName(int itemId)
        {
            string key = string.Format(@"ITEM_{0}", StringUtils.Digit3(itemId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetMagicName(int magicId)
        {
            string key = string.Format(@"MAGIC_NAME_{0}", StringUtils.Digit3(magicId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetOccupationName(int occupationId)
        {
            string key = string.Format(@"OCCUPATION_{0}", StringUtils.Digit3(occupationId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetRaceName(int race)
        {
            string key = string.Format(@"RACE_{0}", StringUtils.Digit3(race));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetVillagePositionName(int positionId)
        {
            string key = string.Format(@"VILLAGE_{0}", StringUtils.Digit3(positionId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetChapterTitle(int chapterId)
        {
            string key = string.Format(@"CHAPTER_{0}", StringUtils.Digit3(chapterId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetChapterWinCondition(int chapterId)
        {
            string key = string.Format(@"CHAPTER_WIN_{0}", StringUtils.Digit3(chapterId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetChapterLostCondition(int chapterId)
        {
            string key = string.Format(@"CHAPTER_LOST_{0}", StringUtils.Digit3(chapterId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetConversationString(int chapterId, int conversationId, int sequenceId)
        {
            string key = string.Format(@"CONV_{0}_{1}_{2}", 
                StringUtils.Digit2(chapterId), StringUtils.Digit2(conversationId), StringUtils.Digit3(sequenceId));
            return LanguageManager.Instance.GetTextValue(key);
        }







    }
}
