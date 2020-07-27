using FigAniTool2.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class FieldMap
    {

        public int[,] ShapeMatrix
        {
            get; set;
        }

        public int Width
        {
            get; set;
        }

        public int Height
        {
            get; set;
        }

        public int Index
        {
            get; set;
        }

        public FieldMap()
        {

        }

        public FieldMap(int index, int width, int height)
        {
            this.Index = index;
            this.Width = width;
            this.Height = height;

            // this.shapePanel = shapePanel;
            this.ShapeMatrix = new int[width, height];
        }

        public int GetShapeIndexAt(int x, int y)
        {
            return this.ShapeMatrix[x, y];
        }

        public void SetShapeAt(int x, int y, int shapeIndex)
        {
            this.ShapeMatrix[x, y] = shapeIndex;
        }

        public HashSet<int> GetAllShapeIndexes()
        {
            HashSet<int> shapes = new HashSet<int>();
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    var shapeIndex = this.GetShapeIndexAt(i, j);
                    shapes.Add(shapeIndex);
                }
            }
            return shapes;
        }


    }
}
