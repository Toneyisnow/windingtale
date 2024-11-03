


using NUnit.Framework.Internal;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using WindingTale.Core.Common;

public class LocalizationManager
{
    public static LocalizedString GetCreatureString(int definitionId)
    {
        string key = string.Format(@"Creature-{0}", StringUtils.Digit3(definitionId));
        return GetString("CommonStrings", key);
    }

    public static LocalizedString GetRaceString(int raceId)
    {
        string key = string.Format(@"Race-{0}", StringUtils.Digit3(raceId));
        return GetString("CommonStrings", key);
    }

    public static LocalizedString GetOccupationString(int raceId)
    {
        string key = string.Format(@"Occupation-{0}", StringUtils.Digit3(raceId));
        return GetString("CommonStrings", key);
    }

    public static LocalizedString GetItemString(int itemId)
    {
        string key = string.Format(@"Item-{0}", StringUtils.Digit3(itemId));
        return GetString("CommonStrings", key);
    }

    public static LocalizedString GetMagicString(int magicId)
    {
        string key = string.Format(@"Magic-{0}", StringUtils.Digit3(magicId));
        return GetString("CommonStrings", key);
    }

    public static LocalizedString GetMessageString(int messageId)
    {
        string key = string.Format(@"Message-{0}", StringUtils.Digit3(messageId));
        return GetString("CommonStrings", key);
    }

    public static LocalizedString GetPromptString(int promptId)
    {
        string key = string.Format(@"Prompt-{0}", StringUtils.Digit3(promptId));
        return GetString("CommonStrings", key);
    }

    private static LocalizedString GetString(string tableName, string entryKey)
    {
        LocalizedString stringReference = new LocalizedString(tableName, entryKey);
        return stringReference;
    }


}