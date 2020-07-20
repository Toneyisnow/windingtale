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

            FieldMap map = fieldData.GetField(4);
            for(int x = 0; x < map.Width; x++)
            {
                for(int y = 0; y < map.Height; y++)
                {
                    ShapeInfo shape = map.GetShapeAt(x, y);
                    if (shape == null)
                    {
                        continue;
                    }

                    ImageDataExporter exporter = new ImageDataExporter(shape.Image);
                    exporter.ExportToPng(string.Format(@"D:\Temp\FDII\Shapes\Shape_{0}_{1}.png", x, y));
                }
            }
        }
    }
}
