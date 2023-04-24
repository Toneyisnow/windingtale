namespace WindingTale.Core.Map
{
    public class MapBlock
    {
        public int BlockType { get; private set; }

        public int BlockId { get; private set; }

        public int BackgroundId { get; private set; }


        public MapBlock(int blockType, int blockId, int backgroundId)
        {
            this.BlockType = blockType;
            this.BlockId = blockId;
            this.BackgroundId = backgroundId;
        }

        public MapBlock() { }
    }
}