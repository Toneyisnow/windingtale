using WindingTale.Core.Definitions;

namespace WindingTale.Core.Map
{
    /// <summary>
    /// A matrix of ShapeDefinition
    /// </summary>
    public class GameField
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private ShapeDefinition[,] shapes = null;

        public GameField(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public ShapeDefinition GetShapeAt(int x, int y)
        {
            if (shapes == null)
            {
                return null;
            }

            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return null;
            }

            return shapes[x, y];
        }
    }
}