
namespace WindingTale.Core.Objects
{
    public class FDTreasure : FDObject
    {
        public int ItemId { get; private set; }
        public int Money { get; private set; }

        public FDTreasure(int id, int itemId) : base(id, ObjectType.Treature)
        {
            this.ItemId = itemId;
        }

    }
}