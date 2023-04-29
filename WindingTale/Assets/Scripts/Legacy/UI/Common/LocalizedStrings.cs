using SmartLocalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WindingTale.Common;

namespace WindingTale.UI.Common
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

        public static string GetConversationString(Conversation conversationId)
        {
            string key = string.Format(@"Chapter-{0}-{0}-{1}-{2}", 
                StringUtils.Digit2(conversationId.ChapterId), StringUtils.Digit2(conversationId.SequenceId), StringUtils.Digit3(conversationId.Index));
            return LanguageManager.Instance.GetTextValue(key);
        }

        public static int GetConversationCreatureId(Conversation conversationId)
        {
            string key = string.Format(@"Chapter-{0}-{0}-{1}-{2}-Id",
                StringUtils.Digit2(conversationId.ChapterId), StringUtils.Digit2(conversationId.SequenceId), StringUtils.Digit3(conversationId.Index));
            string creatureIdString = LanguageManager.Instance.GetTextValue(key);

            int creatureId = 0;
            int.TryParse(creatureIdString, out creatureId);
            return creatureId;
        }

        /// <summary>
        /// Including  Message and Confirm
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public static string GetMessageString(FDMessage messageId)
        {
            string key = string.Format(@"Message-{0}-{1}", messageId.MessageType.ToString(), StringUtils.Digit2(messageId.Key));

            string messageTemplate = LanguageManager.Instance.GetTextValue(key);
            string message = messageTemplate;
            if(messageId.MessageType == FDMessage.MessageTypes.Information)
            {
                switch(messageId.Key)
                {
                    //打开宝箱，发现%@！
                    case 3:
                    case 4:
                        message = string.Format(messageTemplate, GetItemName(messageId.IntParam1));
                        break;
                    // 获得经验值%d点！
                    case 5:
                        message = string.Format(messageTemplate, messageId.IntParam1);
                        break;
                    // 得到%@，将%@放了回去！
                    case 6:
                        message = string.Format(messageTemplate, GetItemName(messageId.IntParam1), GetItemName(messageId.IntParam2));
                        break;
                    // 等级上升了！ 生命值提升 - 速度提升
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                        message = string.Format(messageTemplate, messageId.IntParam1);
                        break;
                    // 学会了XXX术
                    case 17:
                        message = string.Format(messageTemplate, GetMagicName(messageId.IntParam1));
                        break;
                    // 从敌人身上获得%@！
                    case 21:
                        message = string.Format(messageTemplate, GetItemName(messageId.IntParam1));
                        break;

                    // %@带不动了！
                    case 62:
                        message = string.Format(messageTemplate, GetCreatureName(messageId.IntParam1));
                        break;
                    // %@ 转职成 %@
                    case 70:
                        message = string.Format(messageTemplate, GetCreatureName(messageId.IntParam1), GetOccupationName(messageId.IntParam2));
                        break;
                    // 请选择出场的队员：%02d / %02d
                    case 71:
                        message = string.Format(messageTemplate, messageId.IntParam1, messageId.IntParam2);
                        break;

                    default:
                        break;
                }
            }
            
            if (messageId.MessageType == FDMessage.MessageTypes.Confirm)
            {
                switch(messageId.Key)
                {
                    // 这个%@，#%d元，要不要啊？
                    case 54:
                        message = string.Format(messageTemplate, GetItemName(messageId.IntParam1), messageId.IntParam2);
                        break;
                    // 这个%@，#%d元，卖不卖啊？
                    case 55:
                        message = string.Format(messageTemplate, GetItemName(messageId.IntParam1), messageId.IntParam2);
                        break;
                    // 复活%@，#需要%d元，确定要复活吗？
                    case 57:
                        message = string.Format(messageTemplate, GetCreatureName(messageId.IntParam1), messageId.IntParam2);
                        break;
                    // %@转职为%@，#需要%d元，确定要转职吗？
                    case 58:
                        message = string.Format(messageTemplate, GetCreatureName(messageId.IntParam1), GetOccupationName(messageId.IntParam2), messageId.IntParam3);
                        break;
                    default:
                        break;
                }
            }

            return message;
        }


    }
}
