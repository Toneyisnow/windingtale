using WindingTale.Legacy.Core.ObjectModels;

namespace WindingTale.Core.Files
{
    
    public class GameRecord
    {
        public int ChapterId
        {
            get; set;
        }

        public FDFriend Friends { get; set; }

        public int TotalMoney { get; set; }

        public static GameRecord LoadFromFile(string fileName)
        {
            return null;
        }

        public static void SaveToFile(string fileName, GameRecord record)
        {
            return;
        }
    }
}