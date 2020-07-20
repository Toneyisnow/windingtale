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
        private ShapePanel shapePanel = null;

        private Dictionary<FDPosition, ShapeInfo> shapeMap = null;

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
            get; private set;
        }

        public FieldMap(int index, ShapePanel shapePanel)
        {
            this.Index = index;
            this.shapePanel = shapePanel;
            this.shapeMap = new Dictionary<FDPosition, ShapeInfo>();
        }

        public ShapeInfo GetShapeAt(int x, int y)
        {
            FDPosition position = new FDPosition(x, y);
            return shapeMap[position];
        }

        public void SetShapeAt(int x, int y, int shapeIndex)
        {
            FDPosition position = new FDPosition(x, y);
            shapeMap[position] = shapePanel.Shapes[shapeIndex];
        }
    }
}
