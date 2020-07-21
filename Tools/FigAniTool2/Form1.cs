using FigAniTool2.Common;
using FigAniTool2.ObjectModels;
using FigAniTool2.Utils;
using System;
using System.Collections.Generic;
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

            FigDataFile figData = new FigDataFile(@".\FIGANI.DAT", palette);
            figData.LoadData();

            ImageDataExporter exporter = new ImageDataExporter(figData.GetAnimation(9).Frames[3].Image);
            exporter.ExportToPng(@"D:\Temp\FDII\Fight_9_3.png");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Reading Palette
            FDPalette palette = new FDPalette(@".\color");

            ShapeDataFile shapeData = new ShapeDataFile(@".\FDSHAP.DAT", palette);
            shapeData.LoadData();

            ImageDataExporter exporter = new ImageDataExporter(shapeData.GetPanel(3).Shapes[2].Image);
            exporter.ExportToPng(@"D:\Temp\FDII\Shape_3_2.png");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Reading Palette
            FDPalette palette = new FDPalette(@".\color");

            ShapeDataFile shapeData = new ShapeDataFile(@".\FDSHAP.DAT", palette);
            shapeData.LoadData();
            
            FieldDataFile fieldData = new FieldDataFile(@".\FDFIELD.DAT");
            fieldData.LoadData(shapeData);

            for (int m = 1; m < 33; m++)
            {
                FieldMap map = fieldData.GetField(m);
                HashSet<int> shapes = new HashSet<int>();
                for (int i = 0; i < map.Width; i++)
                {
                    for (int j = 0; j < map.Height; j++)
                    {
                        var shape = map.GetShapeAt(i, j);
                        shapes.Add(shape.Index);
                    }
                }

                ShapePanel shapePanel = shapeData.GetPanel(m);
                foreach (int sh in shapes)
                {
                    ShapeInfo shape = shapePanel.Shapes[sh];
                    ImageDataExporter exporter = new ImageDataExporter(shape.Image);
                    exporter.ExportToPng(string.Format(@"D:\Temp\FDII\Shapes\Shape_{0}_{1}.png", m, sh));
                }
            }
        }
    }
}
