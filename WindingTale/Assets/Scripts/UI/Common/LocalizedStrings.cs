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
            string key = string.Format(@"Creature-{0}", StringUtils.Digit3(animationId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetItemName(int itemId)
        {
            string key = string.Format(@"Item-{0}", StringUtils.Digit3(itemId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetMagicName(int magicId)
        {
            string key = string.Format(@"Magic-{0}", StringUtils.Digit3(magicId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetOccupationName(int occupationId)
        {
            string key = string.Format(@"Occupation-{0}", StringUtils.Digit3(occupationId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetRaceName(int race)
        {
            string key = string.Format(@"Race-{0}", StringUtils.Digit3(race));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetVillagePositionName(int positionId)
        {
            string key = string.Format(@"Village-VillagePosition-{0}", positionId);
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetChapterTitle(int chapterId)
        {
            string key = string.Format(@"Chapter-{0}-Title-{0}", StringUtils.Digit2(chapterId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetChapterWinCondition(int chapterId)
        {
            string key = string.Format(@"Chapter-{0}-Condition-Win", StringUtils.Digit2(chapterId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetChapterLostCondition(int chapterId)
        {
            string key = string.Format(@"Chapter-{0}-Condition-Lose", StringUtils.Digit2(chapterId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetConversationString(int chapterId, int conversationId, int sequenceId)
        {
            string key = string.Format(@"Chapter-{0}-{0}-{1}-{2}", 
                StringUtils.Digit2(chapterId), StringUtils.Digit2(conversationId), StringUtils.Digit3(sequenceId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetMessageString(int messageId)
        {
            string key = string.Format(@"Message-Message-{0}", StringUtils.Digit2(messageId));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static string GetConfirmString(int confirmId)
        {
            string key = string.Format(@"Message-Confirm-{0}", StringUtils.Digit2(confirmId));
            return LanguageManager.Instance.GetTextValue(key);
        }







    }
}
