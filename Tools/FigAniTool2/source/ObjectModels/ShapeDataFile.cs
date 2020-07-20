using FigAniTool2.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class ShapeDataFile
    {
        private string fileFullName = string.Empty;

        private FDPalette palette = null;

        private List<ShapePanel> ShapePanels = null;

        public ShapeDataFile(string fileFullName, FDPalette palette)
        {
            this.fileFullName = fileFullName;
            this.palette = palette;
        }

        public void LoadData()
        {
            using (FileStream streamData = new FileStream(fileFullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData, Encoding.UTF8))
                {
                    reader.Skip(6);

                    int panelCount = 1;
                    int address0 = reader.ReadInt32();
                    int address1 = reader.ReadInt32();
                    List<int> panelAddresses = new List<int>();
                    List<int> panelControlAddresses = new List<int>();

                    panelAddresses.Add(address0);
                    panelControlAddresses.Add(address1);
                    while (reader.BaseStream.Position < address0)
                    {
                        panelAddresses.Add(reader.ReadInt32());
                        panelControlAddresses.Add(reader.ReadInt32());
                        panelCount++;
                    }

                    this.ShapePanels = new List<ShapePanel>();
                    for (int panelIndex = 0; panelIndex < panelCount - 1; panelIndex++)
                    {
                        if (panelAddresses[panelIndex] >= reader.BaseStream.Length)
                        {
                            break;
                        }

                        reader.Seek(panelAddresses[panelIndex], SeekOrigin.Begin);

                        // Read the Shape Panel
                        ShapePanel panel = new ShapePanel();
                        panel.Index = panelIndex;
                        panel.ShapeWidth = reader.ReadInt16();
                        panel.ShapeHeight = reader.ReadInt16();

                        int shapeCount = reader.ReadInt16();
                        List<int> shapeAddresses = new List<int>();
                        for (int c = 0; c < shapeCount; c++)
                        {
                            shapeAddresses.Add(reader.ReadInt32());
                        }

                        // Image Info
                        for (int c = 0; c < shapeCount; c++)
                        {
                            reader.Seek(panelAddresses[panelIndex] + shapeAddresses[c]);

                            ShapeInfo shape = new ShapeInfo();
                            shape.Image = FDImage.ReadFromBinary(reader, palette, panel.ShapeWidth, panel.ShapeHeight);
                            panel.Shapes.Add(shape);
                        }

                        // Control Info
                        reader.Seek(panelControlAddresses[panelIndex]);
                        for (int c = 0; c < shapeCount; c++)
                        {
                            ShapeInfo shape = panel.Shapes[c];

                            shape.EventId = reader.ReadByte();
                            shape.Type = (ShapeInfo.ShapeType)reader.ReadByte();
                            shape.BattleGroundId = reader.ReadInt16();
                        }

                        this.ShapePanels.Add(panel);
                    }
                }
            }
        }

        public ShapePanel GetPanel(int index)
        {
            if (this.ShapePanels == null || this.ShapePanels.Count <= index || index < 0)
            {
                return null;
            }

            return this.ShapePanels[index];
        }

    }
}
