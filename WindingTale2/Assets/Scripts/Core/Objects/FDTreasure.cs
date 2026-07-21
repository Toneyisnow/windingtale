
using WindingTale.Core.Definitions;

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

        /// <summary>
        /// Which chest to draw: RedBox / BlueBox pick the model, Hidden draws nothing
        /// (see ObjectsLayer). Carried on the runtime object, not just the definition,
        /// because a map restored from a save has no chapter definition to consult.
        /// </summary>
        public TreasureType Type { get; private set; }

        public FDTreasure(int id, int itemId, TreasureType type = TreasureType.RedBox)
            : base(id, ObjectType.Treature)
        {
            this.ItemId = itemId;
            this.HasOpened = false;
            this.Type = type;
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
