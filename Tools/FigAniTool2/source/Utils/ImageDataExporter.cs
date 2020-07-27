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
    public class ImageDataExporter
    {
        private FDBitmap bitmapData = null;


        public ImageDataExporter(FDBitmap data)
        {
            this.bitmapData = data;
        }

        public void ExportToPng(string pngFileFullPath, byte rotate = 0)
        {
            using (FileStream output = new FileStream(pngFileFullPath, FileMode.Create, FileAccess.Write))
            {
                unsafe
                {
                    /*
                    byte[] updatedData = new byte[imageData.Width * imageData.Height * 4];
                    for (int i = 0; i < imageData.Width * imageData.Height; i++)
                    {
                        int val = i << 2;
                        updatedData[val] = imageData.RawData[val];
                        updatedData[val + 1] = imageData.RawData[val + 1];
                        updatedData[val + 2] = imageData.RawData[val + 2];
                        updatedData[val + 3] = (byte)((byte)255 - imageData.RawData[val + 3]); // The Alpha value, for PNG format A=255 is solid but in Unity A==255 is transparent
                    }
                    */

                    byte[] updatedData = bitmapData.RawData;
                    fixed (byte* ptr = updatedData)
                    {
                        using (Bitmap image = new Bitmap(bitmapData.Width, bitmapData.Height, bitmapData.Width * 4, PixelFormat.Format32bppPArgb, new IntPtr(ptr)))
                        {
                            image.Save(output, ImageFormat.Png);
                        }
                    }
                }
            }
        }
    }
}
