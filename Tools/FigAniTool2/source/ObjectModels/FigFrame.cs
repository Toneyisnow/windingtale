using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class FigFrame
    {
        public FigFrame()
        {
            
        }



        public int PositionX
        {
            get; set;
        }

        public int PositionY
        {
            get; set;
        }

        public bool IsHit
        {
            get; set;
        }

        public bool IsRemote
        {
            get; set;
        }

        public int EffectAudioId
        {
            get; set;
        }

        public int Latency
        {
            get; set;
        }

        public FDImage Image
        {
            get; set;
        }

    }
}
