using WindingTale.Core.Objects;

namespace WindingTale.Core.Files
{
    
    public class GameRecordManager
    {
        public int ChapterId
        {
            get; set;
        }

        public FDFriend Friends { get; set; }

        public int TotalMoney { get; set; }

        public GameRecord LoadFromFile(string recordName)
        {
            return null;
        }

        public void SaveToFile(string recordName, GameRecord record)
        {
            return;
        }
    }
}