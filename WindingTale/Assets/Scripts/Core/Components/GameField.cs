using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.Components
{
    /// <summary>
    /// Doing the field blocks operation
    /// </summary>
    public class GameField
    {
        public int Width
        {
            get; private set;
        }

        public int Height
        {
            get; private set;
        }

        public int[,] Map
        {
            get; private set;
        }

        public Dictionary<int, ShapeDefinition> Shapes
        {
            get; private set;
        }

        public ShapeDefinition GetShapeAt(int posX, int posY)
        {
            int shapeIndex = this.Map[posX, posY];
            if (this.Shapes.ContainsKey(shapeIndex))
            {
                return this.Shapes[shapeIndex];
            }

            return null;
        }

        public List<TreasureDefinition> Treasures
        {
            get; set;
        }

        /// <summary>
        /// Create the field with ChapterDefinition
        /// </summary>
        /// <param name="chapterDefinition"></param>
        public GameField(ChapterDefinition chapterDefinition)
        {
            this.Width = chapterDefinition.Width;
            this.Height = chapterDefinition.Height;
            this.Map = chapterDefinition.Map;
            this.Shapes = chapterDefinition.ShapeDict;

        }
    }
}