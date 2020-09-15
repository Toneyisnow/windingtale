using FigAniTool2.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class DatoDataFile
    {
        private string fileFullName = string.Empty;

        public List<FDImage[]> DatoImages
        {
            get; private set;
        }

        public DatoDataFile(string fileFullName)
        {
            this.fileFullName = fileFullName;
        }

        public void LoadData()
        {
            this.DatoImages = new List<FDImage[]>();

            using (FileStream streamData = new FileStream(fileFullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData, Encoding.UTF8))
                {
                    reader.Skip(6);

                    int address0 = reader.ReadInt32();

                    List<int> creatureAddresses = new List<int>();
                    creatureAddresses.Add(address0);
                    while (reader.BaseStream.Position < address0)
                    {
                        creatureAddresses.Add(reader.ReadInt32());
                    }

                    for(int creature = 0; creature < creatureAddresses.Count - 1; creature ++)
                    {
                        int baseAddress = creatureAddresses[creature];
                        reader.Seek(baseAddress, SeekOrigin.Begin);

                        List<int> biases = new List<int>();
                        for(int bias = 0; bias < 4; bias ++)
                        {
                            biases.Add(reader.ReadInt32());
                        }

                        FDImage[] images = new FDImage[4];
                        for (int bias = 0; bias < 4; bias++)
                        {
                            reader.Seek(baseAddress + biases[bias], SeekOrigin.Begin);

                            int width = reader.ReadInt16();
                            int height = reader.ReadInt16();

                            images[bias] = FDImage.ReadFromBinary(reader, width, height);
                        }

                        this.DatoImages.Add(images);
                    }
                }
            }
        }
    }
}
