using FigAniTool2.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.ObjectModels
{
    public class FDImage
    {
        public int Height
        {
            get; set;
        }

        public int Width
        {
            get; set;
        }

        private byte[] rawData;
        private int dataIndex = 0;

        public FDImage(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.rawData = new byte[width * height];
            this.dataIndex = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="palette"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static FDImage ReadFromBinary(BinaryReader reader, int width = 0, int height = 0)
        {
            if (width == 0 || height == 0)
            {
                width = reader.ReadInt16();
                height = reader.ReadInt16();
            }

            FDImage image = new FDImage(width, height);

            // Start reading frame image
            while (!image.IsCompleted)
            {
                byte cbyte = reader.ReadByte();

                if ((cbyte & 0xC0) == 0xC0) // Skip N 
                {
                    int count = cbyte - 0xC0 + 1;
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData(0);  // write empty data
                    }
                }
                else if ((cbyte & 0x80) == 0x80)
                {
                    int count = cbyte - 0x80 + 1;
                    for (int i = 0; i < count; i++)
                    {
                        byte colorIndex = reader.ReadByte();
                        image.WriteData(colorIndex);
                    }
                }
                else if ((cbyte & 0x40) == 0x40)
                {
                    int count = cbyte - 0x40 + 1;
                    byte colorIndex = reader.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData(0);
                        image.WriteData(colorIndex);  // write plain data from palette
                    }
                }
                else
                {
                    int count = cbyte + 1;
                    byte colorIndex = reader.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData(colorIndex);  // write plain data from palette
                    }
                }
            }

            return image;
        }

        public static FDImage ReadPlainFromBinary(BinaryReader reader, int width = 0, int height = 0)
        {
            if (width == 0 || height == 0)
            {
                width = reader.ReadInt16();
                height = reader.ReadInt16();
            }

            FDImage image = new FDImage(width, height);

            // Start reading frame image
            while (!image.IsCompleted)
            {
                byte cbyte = reader.ReadByte();

                if ((cbyte & 0xC0) == 0xC0) // Skip N 
                {
                    int count = cbyte - 0xC0 + 1;
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData(0);  // write empty data
                    }
                }
                else if ((cbyte & 0x80) == 0x80)
                {
                    int count = cbyte - 0x80 + 1;
                    for (int i = 0; i < count; i++)
                    {
                        byte colorIndex = reader.ReadByte();
                        image.WriteData(colorIndex);
                    }
                }
                else if ((cbyte & 0x40) == 0x40)
                {
                    int count = cbyte - 0x40 + 1;
                    byte colorIndex = reader.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData(0);
                        image.WriteData(colorIndex);  // write plain data from palette
                    }
                }
                else
                {
                    int count = cbyte + 1;
                    byte colorIndex = reader.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData(colorIndex);  // write plain data from palette
                    }
                }
            }

            return image;
        }

        public void WriteData(byte colorIndex)
        {
            if (dataIndex < Width * Height)
            {
                this.rawData[this.dataIndex++] = colorIndex;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return dataIndex >= Width * Height;
            }
        }

        public byte GetPixelColorIndex(int x, int y, byte rotation = 0)
        {
            if (x < 0 || x >= this.Width || y < 0 || y >= this.Height)
            {
                throw new IndexOutOfRangeException();
            }

            return rawData[x + y * this.Width];
        }

        public byte[] RawData
        {
            get
            {
                return rawData;
            }
        }

        /// <summary>
        /// This is the function to generate the Bitmap for exporting
        /// </summary>
        /// <param name="palette"></param>
        /// <returns></returns>
        public FDBitmap GenerateBitmap(FDPalette palette)
        {
            FDBitmap bitmap = new FDBitmap(this.Width, this.Height);

            for(int i = 0; i < this.Width * this.Height; i++)
            {
                byte colorIndex = this.RawData[i];
                if (colorIndex > 0)
                {
                    Bit24Color color = palette.At(colorIndex);
                    bitmap.WriteData((byte)(color.Red << 2), (byte)(color.Green << 2), (byte)(color.Blue << 2), 255);
                }
                else
                {
                    bitmap.WriteData(0, 0, 0, 0);
                }
            }

            return bitmap;
        }

        public byte[] SerializeToBytes()
        {
            byte[] widthBytes = BitConverter.GetBytes(this.Width);
            byte[] heightBytes = BitConverter.GetBytes(this.Height);

            IEnumerable<byte> rv = widthBytes.Concat(heightBytes).Concat(rawData);
            return rv.ToArray();
        }

        public void DeserializeFromBytes(byte[] data)
        {
            this.Width = BitConverter.ToInt32(data, 0);
            this.Height = BitConverter.ToInt32(data, 4);

            this.rawData = new byte[Width * Height * 4];
            int length = data.Length - 8;

            Array.Copy(data, 8, rawData, 0, length);
        }

    }
}
