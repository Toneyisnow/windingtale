using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigAniTool2.Common
{
    public static class BinaryReaderExtension
    {
        public static void Skip(this BinaryReader reader, int count)
        {
            reader.ReadBytes(count);
        }


        public static void Seek(this BinaryReader reader, int count, SeekOrigin origin = SeekOrigin.Begin)
        {
            reader.BaseStream.Seek(count, origin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="byteCount"></param>
        /// <param name="limit"></param>
        /// <param name="negate"></param>
        public static void ReadBitMask(this BinaryReader reader, HashSet<int> dest, int byteCount, int valueCount, bool negate = true)
        {
            bool[] boolDest = new bool[valueCount];
            reader.ReadBitMask(boolDest, byteCount, valueCount, negate);

            for (int i = 0; i < valueCount; i++)
            {
                if (boolDest[i])
                {
                    dest.Add(i);
                }
            }
        }

        public static void ReadBitMask(this BinaryReader reader, bool[] dest, int byteCount, int valueCount, bool negate)
        {
            for (int nowByte = 0; nowByte < byteCount; nowByte++)
            {
                int mask = reader.ReadByte();
                for (int bit = 0; bit < 8; ++bit)
                {
                    if (nowByte * 8 + bit < valueCount)
                    {
                        bool flag = (mask & (1 << bit)) > 0;
                        dest[nowByte * 8 + bit] = (flag != negate);        // FIXME: check PR388
                    }
                }
            }
        }

        public static string ReadStringWithLength(this BinaryReader reader)
        {
            UInt32 length = reader.ReadUInt32();
            if (length > 4096)
            {
                //// throw new ArgumentOutOfRangeException("ReadStringWithLength: length is out of range: " + length);
                ///
                reader.Seek(-4, SeekOrigin.Current);
                return ReadStringToEnd(reader);
            }

            if (length == 0)
            {
                return string.Empty;
            }

            byte[] result = new byte[length];
            for (var i = 0; i < length; i++)
            {
                result[i] = reader.ReadByte();

                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                {
                    // If the reader is beyond the file length, just skip
                    break;
                }
            }

            return Encoding.ASCII.GetString(result);
        }


        public static string ReadStringToEnd(this BinaryReader reader)
        {
            byte[] str = new byte[4096];
            int length = 0;
            bool isQuoted = false;

            if (str[0] == '\"')
            {
                isQuoted = true;
            }

            for (var i = 0; i < 4096; i++)
            {
                try
                {
                    str[i] = reader.ReadByte();
                    if (isQuoted && str[i] == '\"'
                        || !isQuoted && (str[i] == '\t' || str[i] == '\r'))
                    {
                        length = isQuoted ? i - 1 : i;
                        break;
                    }
                }
                catch (Exception)
                {
                    length = isQuoted ? i - 1 : i;
                    break;
                }
            }

            if (length == 0)
            {
                return string.Empty;
            }

            byte[] result = new byte[length];
            Buffer.BlockCopy(str, (isQuoted ? 1 : 0), result, 0, length);

            return Encoding.ASCII.GetString(result);
        }


    }
}
