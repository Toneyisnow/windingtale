
namespace WindingTale.Core.Objects
{
    public class FDTreasure : FDObject
    {
        /// <summary>
        /// A chest holds an item, and money is itself an item (a MoneyItemDefinition, whose
        /// Amount is the sum). So there is no separate money field to carry.
        /// </summary>
        public int ItemId { get; private set; }

        public bool HasOpened { get; private set; }

        public FDTreasure(int id, int itemId) : base(id, ObjectType.Treature)
        {
            this.ItemId = itemId;
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