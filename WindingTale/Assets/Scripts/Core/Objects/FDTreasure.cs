
namespace WindingTale.Core.Objects
{
    public class FDTreasure : FDObject
    {
        public FDItem Item { get; private set; }
        public int Money { get; private set; }

        public FDTreasure(int id, FDItem item) : base(id, ObjectType.Treature)
        {
            Item = item;
        }

        public FDTreasure(int id, int money) : base(id, ObjectType.Treature)
        {
            Money = money;
        }
    }
}