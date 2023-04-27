namespace WindingTale.Core.Map
{
    /// <summary>
    /// 
    /// </summary>
    public class GameField
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private MapBlock[,] blocks = null;




        public GameField(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public MapBlock GetBlock(int x, int y)
        {
            if (blocks == null)
            {
                return null;
            }

            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return null;
            }

            return blocks[x, y];
        }

    }
}