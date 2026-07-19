
namespace WindingTale.Core.Objects
{
    public class FDTreasure : FDObject
    {
        public int ItemId { get; private set; }
        public int Money { get; private set; }

        public bool HasOpened { get; private set; }

        public FDTreasure(int id, int itemId) : this(id, itemId, 0)
        {
        }

        public FDTreasure(int id, int itemId, int money) : base(id, ObjectType.Treature)
        {
            this.ItemId = itemId;
            this.Money = money;
            this.HasOpened = false;
        }

        public void Open()
        {
            this.HasOpened = true;
        }

        public void UpdateItem(int itemId)
        {
            this.ItemId = itemId;
        }

    }
}