using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class ShapePanel
    {
        public int Index
        {
            get; set;
        }

        public int ShapeWidth
        {
            get; set;
        }

        public int ShapeHeight
        {
            get; set;
        }

        public List<ShapeInfo> Shapes
        {
            get; private set;
        }

        public ShapePanel()
        {
            this.Shapes = new List<ShapeInfo>();
        }




    }
}
