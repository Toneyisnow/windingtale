using FigAniTool2.ObjectModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.Utils
{
    public class FieldMapExporter
    {
        private int unitWidth = 24;
        private int unitHeight = 24;

        private int canvasWidth = 0;
        private int canvasHeight = 0;

        private byte[] canvasData;

        private FieldMap fieldMap = null;

        private ShapePanel shapePanel = null;

        private FDPalette palette = null;

        public FieldMapExporter(FieldMap map, ShapePanel shapePanel, FDPalette palette)
        {
            this.fieldMap = map;
            this.shapePanel = shapePanel;
            this.palette = palette;

            this.canvasWidth = (map.Width + 2) * unitWidth;
            this.canvasHeight = (map.Height + 2) * unitHeight;
        }

        public void ExportToPng(string pngFileFullPath)
        {
            canvasData = new byte[canvasWidth * canvasHeight * 4];
            for (int xx = 0; xx < fieldMap.Width; xx++)
            {
                for (int yy = 0; yy < fieldMap.Height; yy++)
                {
                    var shapeIndex = fieldMap.GetShapeIndexAt(xx, yy);
                    if (shapeIndex < 0)
                    {
                        continue;
                    }

                    ShapeInfo shape = shapePanel.Shapes[shapeIndex];
                    if (shape == null)
                    {
                        continue;
                    }

                    DrawOnCanvas(shape.Image.GenerateBitmap(palette), xx, yy);
                }
            }

            Image[] blockTypes = new Image[6]
            {
                Image.FromFile(@".\resources\MapBlock_0.png"),
                Image.FromFile(@".\resources\MapBlock_1.png"),
                Image.FromFile(@".\resources\MapBlock_2.png"),
                Image.FromFile(@".\resources\MapBlock_3.png"),
                Image.FromFile(@".\resources\MapBlock_4.png"),
                Image.FromFile(@".\resources\MapBlock_5.png")
            };
            
            using (FileStream output = new FileStream(pngFileFullPath, FileMode.Create, FileAccess.Write))
            {
                unsafe
                {
                    fixed (byte* ptr = canvasData)
                    {
                        using (Bitmap image = new Bitmap(canvasWidth, canvasHeight, canvasWidth * 4, PixelFormat.Format32bppPArgb, new IntPtr(ptr)))
                        {
                            using (Graphics g = Graphics.FromImage(image))
                            {

                                for (int xx = 0; xx < fieldMap.Width; xx++)
                                {
                                    for (int yy = 0; yy < fieldMap.Height; yy++)
                                    {
                                        var shapeIndex = fieldMap.GetShapeIndexAt(xx, yy);
                                        if (shapeIndex < 0)
                                        {
                                            continue;
                                        }

                                        ShapeInfo shape = shapePanel.Shapes[shapeIndex];
                                        if (shape == null)
                                        {
                                            continue;
                                        }

                                        int blockType = shape.Type.GetHashCode();
                                        g.DrawImage(blockTypes[blockType], 24 * (xx + 1), 24 * (yy + 1), 24, 24);
                                    }
                                }
                            }
                            
                            image.Save(output, ImageFormat.Png);
                        }
                    }
                }
            }
        }

        private void DrawOnCanvas(FDBitmap imageData, int posX, int posY, byte rotation = 0)
        {
            if (imageData == null)
            {
                return;
            }

            int canvasX = (posX + 2) * unitWidth - 1;
            int canvasY = (posY + 2) * unitHeight - 1;

            for (int i = imageData.Width - 1; i >= 0; i--)
            {
                for (int j = imageData.Height - 1; j >= 0; j--)
                {
                    int targetX = canvasX + i + 1 - imageData.Width;
                    int targetY = canvasY + j + 1 - imageData.Height;

                    if (targetX < 0 || targetY < 0 || targetX >= canvasWidth || targetY >= canvasHeight)
                    {
                        continue;
                    }

                    int targetIndex = (targetY * canvasWidth + targetX) << 2;

                    Color clr = imageData.GetPixelColor(i, j, rotation);

                    // For transparent color, just skip
                    if (clr.A == 0 && clr.R == 0 && clr.G == 255 && clr.B == 255)
                    {
                        continue;
                    }

                    if (clr.A != 0 && clr.A != 255)
                    {
                        canvasData[targetIndex] = (byte)((canvasData[targetIndex] + clr.R) / 2);
                        canvasData[targetIndex + 1] = (byte)((canvasData[targetIndex] + clr.G) / 2);
                        canvasData[targetIndex + 2] = (byte)((canvasData[targetIndex] + clr.B) / 2);
                        canvasData[targetIndex + 3] = 255;
                    }
                    else
                    {
                        canvasData[targetIndex] = clr.R;
                        canvasData[targetIndex + 1] = clr.G;
                        canvasData[targetIndex + 2] = clr.B;
                        canvasData[targetIndex + 3] = clr.A;
                    }
                }
            }
        }
    }
}
