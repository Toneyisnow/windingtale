using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Map
{
    public class FDField
    {

        // Which chapter the field belongs to. The remastered tile models are stored
        // per chapter (Shapes/Shapes_02/Shape_2_*), so the layers need it to pick
        // the right set.
        public int ChapterId { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        private ShapeDefinition[,] shapes = null;

        // Tile ids to draw, which are not the ids to play on: a tile a house was painted
        // onto keeps its Blocked/Forest ShapeType in `shapes` but draws plain ground here,
        // because the house is now an obstacle model standing on top of it.
        private int[,] renderShapeIds = null;

        public List<ObstacleDefinition> Obstacles { get; private set; }

        public FDField(ChapterDefinition chapterDefinition)
        {
            ChapterId = chapterDefinition.ChapterId;
            Width = chapterDefinition.Width;
            Height = chapterDefinition.Height;

            Obstacles = chapterDefinition.Obstacles ?? new List<ObstacleDefinition>();

            // A chapter that has not been remastered has no RenderMatrix; it draws the
            // painted map, buildings and all.
            int[,] renderMap = chapterDefinition.RenderMap ?? chapterDefinition.Map;

            shapes = new ShapeDefinition[Width, Height];
            renderShapeIds = new int[Width, Height];
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    int shapeId = chapterDefinition.Map[i, j];
                    ShapeDefinition shape = chapterDefinition.ShapeDict[shapeId];
                    shape.Id = shapeId;
                    shapes[i, j] = shape;
                    renderShapeIds[i, j] = renderMap[i, j];
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

        /// <summary>
        /// The tile id to draw at this position. Use it only to pick a model -- everything
        /// the battle reads (move cost, terrain bonuses) comes from
        /// <see cref="GetShapeAt"/>, which still reports the painted terrain.
        /// </summary>
        public int GetRenderShapeIdAt(FDPosition position)
        {
            if (renderShapeIds == null)
            {
                return -1;
            }

            if (position.X < 1 || position.X > Width || position.Y < 1 || position.Y > Height)
            {
                return -1;
            }

            return renderShapeIds[position.X - 1, position.Y - 1];
        }
    }
}
