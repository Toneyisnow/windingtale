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
        private FDPalette palette = null;

        public int Height
        {
            get; set;
        }

        public int Width
        {
            get; set;
        }

        private byte[] rawData;

        private Dictionary<byte, byte[]> plainData = null;

        private int dataIndex = 0;

        public FDImage()
        {
            this.plainData = new Dictionary<byte, byte[]>();
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
        public static FDImage ReadFromBinary(BinaryReader reader, FDPalette palette, int width = 0, int height = 0)
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
                        image.WriteData(0, 0, 0, 0);  // write empty data
                    }
                }
                else if ((cbyte & 0x80) == 0x80)
                {
                    int count = cbyte - 0x80 + 1;
                    for (int i = 0; i < count; i++)
                    {
                        int colorIndex = reader.ReadByte();
                        Bit24Color color = palette.At(colorIndex);
                        image.WriteData((byte)(color.Red << 2), (byte)(color.Green << 2), (byte)(color.Blue << 2), 255);  // write plain data from palette
                    }
                }
                else if ((cbyte & 0x40) == 0x40)
                {
                    int count = cbyte - 0x40 + 1;
                    int colorIndex = reader.ReadByte();
                    Bit24Color color = palette.At(colorIndex);
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData(0, 0, 0, 0);
                        image.WriteData((byte)(color.Red << 2), (byte)(color.Green << 2), (byte)(color.Blue << 2), 255);  // write plain data from palette
                    }
                }
                else
                {
                    int count = cbyte + 1;
                    int colorIndex = reader.ReadByte();
                    Bit24Color color = palette.At(colorIndex);
                    for (int i = 0; i < count; i++)
                    {
                        image.WriteData((byte)(color.Red << 2), (byte)(color.Green << 2), (byte)(color.Blue << 2), 255);  // write plain data from palette
                    }
                }
            }

            return image;
        }

        public FDImage(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.rawData = new byte[width * height * 4];
            this.plainData = new Dictionary<byte, byte[]>();

            this.dataIndex = 0;
        }

        public void WriteData(byte red, byte green, byte blue, byte alpha)
        {
            this.rawData[this.dataIndex++] = red;
            this.rawData[this.dataIndex++] = green;
            this.rawData[this.dataIndex++] = blue;
            this.rawData[this.dataIndex++] = alpha;
        }

        public void WriteColor(Color color)
        {
            this.rawData[this.dataIndex++] = color.R;
            this.rawData[this.dataIndex++] = color.G;
            this.rawData[this.dataIndex++] = color.B;
            this.rawData[this.dataIndex++] = color.A;
        }

        public bool IsCompleted
        {
            get
            {
                return dataIndex >= Width * Height * 4;
            }
        }

        public Color GetPixelColor(int x, int y, byte rotation = 0)
        {
            if (x < 0 || x >= this.Width || y < 0 || y >= this.Height)
            {
                throw new IndexOutOfRangeException();
            }

            byte[] data = GetPlainData(rotation);
            int index = (y * this.Width + x) << 2;

            byte r = data[index];
            byte g = data[index + 1];
            byte b = data[index + 2];
            byte a = data[index + 3];

            return Color.FromArgb(a, r, g, b);
        }

        public byte[] RawData
        {
            get
            {
                return rawData;
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotationIndex"></param>
        /// <returns></returns>
        public byte[] GetPlainData(byte rotation = 0)
        {
            if (this.plainData.ContainsKey(rotation))
            {
                return this.plainData[rotation];
            }

            byte[] data = null;
            switch (rotation)
            {
                case 0:
                    data = rawData;
                    break;
                case 1:
                    data = FlipLeftAndRight(rawData);
                    break;
                case 2:
                    data = FlipUpAndDown(rawData);
                    break;
                case 3:
                    data = Rotate180(rawData);
                    break;
                default:
                    return null;
            }

            this.plainData[rotation] = data;
            return data;
        }

        private byte[] Rotate180(byte[] raw)
        {
            byte[] resultData = new byte[Width * Height * 4];

            for (int j = 0; j < this.Height; j++)
            {
                for (int i = 0; i < this.Width; i++)
                {
                    int originIndex = i + Width * j;
                    int newIndex = Width - 1 - i + Width * (Height - j - 1);

                    for (int w = 0; w < 4; w++)
                    {
                        resultData[newIndex * 4 + w] = raw[originIndex * 4 + w];
                    }
                }
            }

            return resultData;
        }

        private byte[] FlipUpAndDown(byte[] raw)
        {
            byte[] resultData = new byte[Width * Height * 4];

            for (int j = 0; j < this.Height; j++)
            {
                for (int i = 0; i < this.Width; i++)
                {
                    int originIndex = i + Width * j;
                    int newIndex = i + Width * (Height - j - 1);

                    for (int w = 0; w < 4; w++)
                    {
                        resultData[newIndex * 4 + w] = raw[originIndex * 4 + w];
                    }
                }
            }

            return resultData;
        }

        private byte[] FlipLeftAndRight(byte[] raw)
        {
            byte[] resultData = new byte[Width * Height * 4];

            for (int j = 0; j < this.Height; j++)
            {
                for (int i = 0; i < this.Width; i++)
                {
                    int originIndex = i + Width * j;
                    int newIndex = Width - 1 - i + Width * j;

                    for (int w = 0; w < 4; w++)
                    {
                        resultData[newIndex * 4 + w] = raw[originIndex * 4 + w];
                    }
                }
            }

            return resultData;
        }
    }
}
