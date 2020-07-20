using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class FigAnimation
    {
        public FigAnimation()
        {
            this.Frames = new List<FigFrame>();
        }

        public string Name
        {
            get; set;
        }

        public List<FigFrame> Frames
        {
            get; set;
        }

        public int RemoteFrameIndex
        {
            get; set;
        }

    }
}
