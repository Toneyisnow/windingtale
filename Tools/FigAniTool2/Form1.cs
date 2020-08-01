using FigAniTool2.Common;
using FigAniTool2.ObjectModels;
using FigAniTool2.Utils;
using FigAniTool2.Voxes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FigAniTool2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Reading Palette
            FDPalette palette = new FDPalette(@".\color");

            FigDataFile figData = new FigDataFile(@".\FIGANI.DAT");
            figData.LoadData();

            ImageDataExporter exporter = new ImageDataExporter(figData.GetAnimation(9).Frames[3].Image.GenerateBitmap(palette));
            exporter.ExportToPng(@"D:\Temp\FDII\Fight_9_3.png");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Reading Palette
            FDPalette palette = new FDPalette(@".\color");

            ShapeDataFile shapeData = new ShapeDataFile(@".\FDSHAP.DAT");
            shapeData.LoadData();

            ImageDataExporter exporter = new ImageDataExporter(shapeData.GetPanel(1).Shapes[100].Image.GenerateBitmap(palette));
            exporter.ExportToPng(@"D:\Temp\FDII\Shape_1_100.png");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            ExportFieldMapData();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CreateVoxPaletteMapping2();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            GenerateAllVoxes();
        }

        private void GenerateAllVoxes()
        {
            FDPalette palette = new FDPalette(@".\color");

            ShapeDataFile shapeData = new ShapeDataFile(@".\FDSHAP.DAT");
            shapeData.LoadData();
            FieldDataFile fieldData = new FieldDataFile(@".\FDFIELD.DAT");
            fieldData.LoadData();

            byte[] header = File.ReadAllBytes(@".\vox_header.dat");
            byte[] footer = File.ReadAllBytes(@".\vox_footer.dat");
            var paletteMapping = LoadVoxPaletteMapping();


            // For each map, generate the vox files
            for (int m = 1; m < 2; m++)
            {
                FieldMap map = fieldData.GetField(m);
                HashSet<int> shapes = map.GetAllShapeIndexes();

                ShapePanel panel = shapeData.GetPanel(m);
                string outputFolder = string.Format(@"D:\GitRoot\toneyisnow\windingtale\Resources\Remastered\Shapes\ShapePanel{0}", m);

                if(!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                //string mappingText = File.ReadAllText(Path.Combine(outputFolder, "shape-mapping-1.json"));
                //ShapeMapping mapping = JsonConvert.DeserializeObject<ShapeMapping>(mappingText);

                foreach (int shape in shapes)
                {
                    //if (mapping.ContainsValue(shape))
                    {
                        // continue;
                    }

                    var shapeInfo = panel.Shapes[shape];
                    string outputFileName = string.Format(@"Shape_{0}_{1}.vox", m, shape);

                    GenerateShapeVoxFile(shapeInfo.Image, Path.Combine(outputFolder, outputFileName), header, footer, paletteMapping);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="voxFullName"></param>
        /// <param name="palette"></param>
        private void GenerateShapeVoxFile(FDImage image, string voxFullName, 
            byte[] header, byte[] footer, Dictionary<byte, byte> paletteMapping)
        {
            using (FileStream streamData = new FileStream(voxFullName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(streamData, Encoding.UTF8))
                {
                    writer.Write(header);
                    writer.Write('X');
                    writer.Write('Y');
                    writer.Write('Z');
                    writer.Write('I');
                    writer.Write((Int64)0x904);
                    writer.Write((int)0x240);

                    for (byte y = 0; y < 24; y++)
                    {
                        for (byte x = 0; x < 24; x++)
                        {
                            byte convertedX = x;
                            byte convertedY = (byte)(23 - y);
                            byte cIndex = image.GetPixelColorIndex(convertedX, convertedY);
                            writer.Write(x);
                            writer.Write(y);
                            writer.Write((byte)23);
                            if (paletteMapping.ContainsKey(cIndex))
                            {
                                writer.Write(paletteMapping[cIndex]);
                            }
                            else
                            {
                                writer.Write(cIndex);
                            }
                        }
                    }

                    writer.Write(footer);
                }
            }
        }

        private void ExportAllUsedShapes()
        {
            // Reading Palette
            FDPalette palette = new FDPalette(@".\color");

            ShapeDataFile shapeData = new ShapeDataFile(@".\FDSHAP.DAT");
            shapeData.LoadData();

            FieldDataFile fieldData = new FieldDataFile(@".\FDFIELD.DAT");
            fieldData.LoadData();

            for (int m = 0; m < 33; m++)
            {
                FieldMap map = fieldData.GetField(m);
                HashSet<int> shapes = map.GetAllShapeIndexes();

                ShapePanel panel = shapeData.GetPanel(m);
                string outputFolder = string.Format(@"D:\GitRoot\toneyisnow\windingtale\Resources\Original\Shapes\ShapePanel{0}\Used", m);
                if(!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                foreach (int shape in shapes)
                {
                    var shapeInfo = panel.Shapes[shape];

                    ImageDataExporter exporter = new ImageDataExporter(shapeInfo.Image.GenerateBitmap(palette));
                    exporter.ExportToPng(Path.Combine(outputFolder, string.Format(@"Shape_{0}_{1}.png", m, shape)));
                }
            }
        }

        private void ExportFieldMapData()
        {
            // Reading Palette
            FDPalette palette = new FDPalette(@".\color");

            ShapeDataFile shapeData = new ShapeDataFile(@".\FDSHAP.DAT");
            shapeData.LoadData();

            FieldDataFile fieldData = new FieldDataFile(@".\FDFIELD.DAT");
            fieldData.LoadData();

            for (int m = 0; m < 33; m++)
            {
                FieldMap map = fieldData.GetField(m);
                HashSet<int> shapes = new HashSet<int>();
                for (int i = 0; i < map.Width; i++)
                {
                    for (int j = 0; j < map.Height; j++)
                    {
                        var shapeIndex = map.GetShapeIndexAt(i, j);
                        shapes.Add(shapeIndex);
                    }
                }

                var shapePanel = shapeData.GetPanel(m);
                foreach(int shapeIndex in shapes)
                {
                    ShapeInfo shapeInfo = shapePanel.Shapes[shapeIndex];

                    FieldShape shape = new FieldShape();
                    shape.Type = shapeInfo.Type;
                    shape.BattleGroundId = shapeInfo.BattleGroundId;
                    map.Shapes[shapeIndex] = shape;
                }

                string mapString = JsonConvert.SerializeObject(map, Formatting.Indented);
                File.WriteAllText(string.Format(@"D:\Temp\FDII\MapData_{0}.dat", m), mapString);
            }
        }


        private Dictionary<byte, byte> LoadVoxPaletteMapping()
        {
            Dictionary<byte, byte> mapping = new Dictionary<byte, byte>();
            using (StreamReader reader = new StreamReader(@"D:\Temp\FDII\paletteMapping3.txt"))
            {
                int count = Int32.Parse(reader.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    string line = reader.ReadLine();
                    string[] sp = line.Split(' ');
                    byte origin = byte.Parse(sp[0]);
                    byte target = byte.Parse(sp[1]);
                    mapping[origin] = target;
                }
            }

            return mapping;
        }

        private Dictionary<byte, byte> CreateVoxPaletteMapping()
        {
            string voxFullName = @"D:\Temp\FDII\vox_test2.vox";
            byte[,] targetmatrix = new byte[24, 24];
            using (FileStream streamData = new FileStream(voxFullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData))
                {
                    reader.Skip(0x3C);
                    for (byte y = 0; y < 24; y++)
                    {
                        for (byte x = 0; x < 24; x++)
                        {
                            byte xx = reader.ReadByte();
                            byte yy = reader.ReadByte();
                            reader.ReadByte();

                            byte cIndex = reader.ReadByte();
                            targetmatrix[xx, yy] = cIndex;
                        }
                    }
                }
            }

            string voxFullName2 = @"D:\Temp\FDII\vox_test_converted.vox";
            Dictionary<byte, byte> paletteMapping = new Dictionary<byte, byte>();
            using (FileStream streamData = new FileStream(voxFullName2, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData))
                {
                    reader.Skip(0x3C);
                    for (byte y = 0; y < 24; y++)
                    {
                        for (byte x = 0; x < 24; x++)
                        {
                            byte xx = reader.ReadByte();
                            byte yy = reader.ReadByte();
                            reader.ReadByte();

                            byte cIndex = reader.ReadByte();
                            paletteMapping[cIndex] = targetmatrix[xx, yy];
                        }
                    }
                }
            }

            using(StreamWriter writer = new StreamWriter(@"D:\Temp\FDII\paletteMapping.txt"))
            {
                writer.WriteLine(paletteMapping.Count);
                foreach(var mapping in paletteMapping)
                {
                    writer.WriteLine(mapping.Key + " " + mapping.Value);
                }
            }

            return paletteMapping;
        }

        private Dictionary<byte, byte> CreateVoxPaletteMapping2()
        {
            Dictionary<byte, byte> paletteMapping = new Dictionary<byte, byte>();

            ShapeDataFile shapeData = new ShapeDataFile(@".\FDSHAP.DAT");
            shapeData.LoadData();

            MakeVoxPaletteMapping(shapeData, 1, 20, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 2, 205, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 3, 24, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 3, 144, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 7, 84, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 12, 237, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 13, 87, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 15, 64, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 18, 41, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 18, 79, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 19, 10, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 21, 57, paletteMapping);
            MakeVoxPaletteMapping(shapeData, 24, 10, paletteMapping);
            // MakeVoxPaletteMapping(shapeData, 25, 22, paletteMapping);

            using (StreamWriter writer = new StreamWriter(@"D:\Temp\FDII\paletteMapping3.txt"))
            {
                writer.WriteLine(paletteMapping.Count);
                List<byte> keys = paletteMapping.Keys.ToList();
                keys.Sort();

                foreach (byte key in keys)
                {
                    writer.WriteLine(key + " " + paletteMapping[key]);
                }
            }

            return paletteMapping;
        }

        private void MakeVoxPaletteMapping(ShapeDataFile shapeData, int panel, int shape, Dictionary<byte, byte> paletteMapping)
        {
            string voxFullName = string.Format(@"D:\Temp\FDII\Shapes\Shape_{0}_{1}.vox", panel, shape);
            byte[,] targetmatrix = new byte[24, 24];
            using (FileStream streamData = new FileStream(voxFullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData))
                {
                    reader.Skip(0x3C);
                    for (byte y = 0; y < 24; y++)
                    {
                        for (byte x = 0; x < 24; x++)
                        {
                            byte xx = reader.ReadByte();
                            byte yy = reader.ReadByte();
                            reader.ReadByte();

                            byte cIndex = reader.ReadByte();
                            targetmatrix[xx, yy] = cIndex;
                        }
                    }
                }
            }

            // Reading Palette
            FDImage image = shapeData.GetPanel(panel).Shapes[shape].Image;
            for (byte y = 0; y < 24; y++)
            {
                for (byte x = 0; x < 24; x++)
                {
                    byte color = image.GetPixelColorIndex(x, y);
                    byte target = targetmatrix[x, 23 - y];

                    if(paletteMapping.ContainsKey(color) && paletteMapping[color] != target)
                    {
                        // Something wrong here
                        int error = 1;
                    }

                    paletteMapping[color] = target;
                }
            }
        }

        private void RenameObjFileNames()
        {
            int panelIndex = 2;
            string voxFolder = string.Format(@"D:\GitRoot\toneyisnow\windingtale\Resources\Remastered\Shapes\ShapePanel{0}", panelIndex);
            string objFolder = string.Format(@"D:\GitRoot\toneyisnow\windingtale\Resources\Remastered\Shapes\ShapePanel{0}\obj", panelIndex);

            string[] voxFiles = Directory.GetFiles(voxFolder, "*.vox");
            List<int> fileNameIndexes = new List<int>();
            foreach(string voxFile in voxFiles)
            {
                string fileName = voxFile.Substring(voxFile.LastIndexOf('\\') + 1);
                string indexStr = fileName.Replace(string.Format(@"Shape_{0}_", panelIndex), "").Replace(".vox", "");
                int index = int.Parse(indexStr);
                fileNameIndexes.Add(index);
            }

            string[] objFiles = Directory.GetFiles(objFolder, "*.obj");
            if (objFiles.Length != voxFiles.Length)
            {
                throw new InvalidOperationException("The count of vox and obj files must be same.");
            }

            int fileIndex = 0;
            foreach(string objFile in objFiles)
            {
                string newFileName = string.Format(@"Shape_{0}_{1}.obj", panelIndex, fileNameIndexes[fileIndex]);
                File.Move(objFile, Path.Combine(objFolder, newFileName));
                fileIndex++;
            }
        }

        private void DumpVoxToMatrix()
        {
            string voxFullName = @"D:\Temp\FDII\vox_test.vox";
            byte[,] matrix = new byte[24, 24];
            using (FileStream streamData = new FileStream(voxFullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(streamData))
                {
                    reader.Skip(0x3C);
                    for (byte y = 0; y < 24; y++)
                    {
                        for (byte x = 0; x < 24; x++)
                        {
                            byte xx = reader.ReadByte();
                            byte yy = reader.ReadByte();
                            reader.ReadByte();

                            byte cIndex = reader.ReadByte();
                            matrix[xx, yy] = cIndex;
                        }
                    }
                }
            }

            using (StreamWriter writer = new StreamWriter(@"D:\Temp\FDII\vox.txt"))
            {
                for (byte y = 0; y < 24; y++)
                {
                    for (byte x = 0; x < 24; x++)
                    {
                        writer.Write(matrix[x,y]);
                        writer.Write(" ");
                    }

                    writer.WriteLine();
                }
            }
        }
    }
}
