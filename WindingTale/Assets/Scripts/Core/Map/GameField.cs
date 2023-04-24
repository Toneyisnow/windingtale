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
    }
}