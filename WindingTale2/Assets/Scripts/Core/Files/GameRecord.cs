using WindingTale.Core.Objects;

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

    }
}