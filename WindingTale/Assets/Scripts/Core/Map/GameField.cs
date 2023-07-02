using UnityEngine.UIElements;
using WindingTale.Core.Common;
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

        public GameField(ChapterDefinition chapterDefinition)
        {
            Width = chapterDefinition.Width;
            Height = chapterDefinition.Height;

            shapes = new ShapeDefinition[Width, Height];
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    int shapeId = chapterDefinition.Map[i, j];
                    ShapeDefinition shape = chapterDefinition.ShapeDict[shapeId];
                    shape.Id = shapeId;
                    shapes[i, j] = shape;
                }
            }
        }

        public ShapeDefinition GetShapeAt(FDPosition position)
        {
            if (shapes == null)
            {
                return null;
            }

            if (position.X < 1 || position.X > Width || position.Y < 1 || position.Y > Height)
            {
                return null;
            }

            return shapes[position.X - 1, position.Y - 1];
        }
    }
}