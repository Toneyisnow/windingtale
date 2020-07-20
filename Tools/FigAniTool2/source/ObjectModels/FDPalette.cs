using FigAniTool2.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class FDPalette
    {
        private Bit24Color[] paletteColors = null;

        public FDPalette(string fileFullName)
        {
            paletteColors = new Bit24Color[256];
            using (FileStream streamData = new FileStream(@".\color", FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData, Encoding.UTF8))
                {
                    for (int i = 0; i < 256; i++)
                    {
                        paletteColors[i] = new Bit24Color();
                        paletteColors[i].Blue = reader.ReadByte();
                        paletteColors[i].Green = reader.ReadByte();
                        paletteColors[i].Red = reader.ReadByte();

                    }
                }
            }
        }

        public Bit24Color At(int index)
        {
            if (paletteColors == null || index < 0 || paletteColors.Length <= index)
            {
                return null;
            }

            return paletteColors[index];
        }



    }
}
