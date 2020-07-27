using FigAniTool2.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class FieldDataFile
    {
        private string fileFullName = null;

        private List<FieldMap> FieldMaps = null;

        public FieldDataFile(string fileFullName)
        {
            this.fileFullName = fileFullName;
        }

        public void LoadData()
        {
            using (FileStream streamData = new FileStream(fileFullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData, Encoding.UTF8))
                {
                    reader.Skip(6);

                    int mapCount = 1;
                    int address0 = reader.ReadInt32();
                    reader.ReadInt32(); // The rest of 2 sections are not too useful here
                    reader.ReadInt32();

                    List<int> mapAddresses = new List<int>();

                    mapAddresses.Add(address0);
                    while (reader.BaseStream.Position < address0)
                    {
                        mapAddresses.Add(reader.ReadInt32());
                        reader.ReadInt32(); // The rest of 2 sections are not too useful here
                        reader.ReadInt32();
                        mapCount++;
                    }

                    this.FieldMaps = new List<FieldMap>();
                    for (int mapIndex = 0; mapIndex < mapCount - 1; mapIndex++)
                    {
                        if (mapAddresses[mapIndex] >= reader.BaseStream.Length)
                        {
                            break;
                        }

                        reader.Seek(mapAddresses[mapIndex], SeekOrigin.Begin);
                        int width = reader.ReadInt16();
                        int height = reader.ReadInt16();

                        FieldMap map = new FieldMap(mapIndex, width, height);
                        
                        for(int y = 0; y < map.Height; y++)
                        {
                            for (int x = 0; x < map.Width; x++)
                            {
                                int shapeIndex = reader.ReadInt16();
                                map.SetShapeAt(x, y, shapeIndex);

                                // Unknown for the control bytes
                                reader.ReadByte();
                                reader.ReadByte();
                            }
                        }

                        this.FieldMaps.Add(map);
                    }
                }
            }
        }

        public FieldMap GetField(int mapIndex)
        {
            if (this.FieldMaps == null || mapIndex < 0 || this.FieldMaps.Count <= mapIndex)
            {
                return null;
            }

            return this.FieldMaps[mapIndex];
        }
    }
}
