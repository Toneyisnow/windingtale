


using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using WindingTale.Core.Common;

public class LocalizationManager
{
    public static LocalizedString GetCreatureString(int definitionId)
    {
        string key = string.Format(@"Creature-{0}", StringUtils.Digit3(definitionId));
        return GetLocalString("CommonStrings", key);
    }

    public static LocalizedString GetRaceString(int raceId)
    {
        string key = string.Format(@"Race-{0}", StringUtils.Digit3(raceId));
        return GetLocalString("CommonStrings", key);
    }

    public static LocalizedString GetOccupationString(int raceId)
    {
        string key = string.Format(@"Occupation-{0}", StringUtils.Digit3(raceId));
        return GetLocalString("CommonStrings", key);
    }

    public static LocalizedString GetItemString(int itemId)
    {
        string key = string.Format(@"Item-{0}", StringUtils.Digit3(itemId));
        return GetLocalString("CommonStrings", key);
    }

    public static LocalizedString GetMagicString(int magicId)
    {
        string key = string.Format(@"Magic-{0}", StringUtils.Digit3(magicId));
        return GetLocalString("CommonStrings", key);
    }

    public static LocalizedString GetConfirmString(int confirmId)
    {
        string key = string.Format(@"Confirm-{0}", StringUtils.Digit2(confirmId));
        return GetLocalString("CommonStrings", key);
    }
    
    public static LocalizedString GetMessageString(int messageId)
    {
        string key = string.Format(@"Message-{0}", StringUtils.Digit2(messageId));
        return GetLocalString("CommonStrings", key);
    }

    public static LocalizedString GetFDMessageString(FDMessage message)
    {
        string key = string.Format(@"Message-{0}", StringUtils.Digit2(message.Key));

        LocalizedString template = GetLocalString("CommonStrings", key);
        var dict = new Dictionary<string, string> { 
            { "IntParam1", message.IntParam1.ToString() },
            { "IntParam2", message.IntParam2.ToString() },
            { "StrParam1", message.StrParam1 },
            { "StrParam2", message.StrParam2 }
        };
        template.Arguments = new object[] { dict };

        return template;
    }

    public static LocalizedString GetConversationString(Conversation conversaction)
    {
        string key = string.Format(@"Conversation-{0}-{1}-{2}",
            StringUtils.Digit2(conversaction.ChapterId),
            StringUtils.Digit2(conversaction.ConversationId),
            StringUtils.Digit2(conversaction.SequenceId));

        string tableName = string.Format(@"ChapterStrings-{0}", StringUtils.Digit2(conversaction.ChapterId));
        return GetLocalString(tableName, key);
    }


    private static LocalizedString GetLocalString(string tableName, string entryKey)
    {
        LocalizedString stringReference = new LocalizedString(tableName, entryKey);
        return stringReference;
    }


}