using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class ShapeInfo
    {
        public enum ShapeType
        {
            Plain = 0,
            Blocked = 1,
            Forest = 2,
            BlackForest = 3,
            Marsh = 4,
            Gap = 5,
        }


        public ShapeInfo()
        {

        }

        public FDImage Image
        {
            get; set;
        }

        public int Index
        {
            get; set;
        }

        /// <summary>
        /// Event on this shape, 02: Treasure box; 04: Hidden treasure box
        /// </summary>
        public int EventId
        {
            get; set;
        }

        public ShapeType Type
        {
            get; set;
        }

        public int BattleGroundId
        {
            get; set;
        }
    }
}
