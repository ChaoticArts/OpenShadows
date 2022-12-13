using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenShadows.FileFormats
{
    public static class Utils
    {
        public static uint SwapEndianess(uint n)
        {
            byte[] bytes = BitConverter.GetBytes(n);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes);
        }

        public static ushort SwapEndianess(ushort n)
        {
            byte[] bytes = BitConverter.GetBytes(n);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes);
        }

        public static byte[] UnpackBoPaCompressedData(byte[] data)
        {
            using var f = new BinaryReader(new MemoryStream(data));

            // skip
            f.ReadBytes(0x0a);

            // unpack BoPa
            uint uncompressedSize = Utils.SwapEndianess(f.ReadUInt32());
            uint compressedSize = Utils.SwapEndianess(f.ReadUInt32());
            var uncompressedData = new byte[uncompressedSize];
            var compressedData = f.ReadBytes((int)compressedSize);

            Utils.UnpackBoPa(compressedData, compressedSize, uncompressedData, uncompressedSize);

            return uncompressedData;
        }

        public static ushort Crc16(byte[] data)
        {
            ushort wCRC = 0;
            for (int i = 0; i < data.Length; i++)
            {
                wCRC ^= (ushort)(data[i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((wCRC & 0x8000) != 0)
                        wCRC = (ushort)((wCRC << 1) ^ 0x1021);
                    else
                        wCRC <<= 1;
                }
            }
            return wCRC;
        }

        public static string ExtractString(BinaryReader br)
        {
            var sb = new StringBuilder();

            byte b;
            do
            {
                b = br.ReadByte();

                switch (b)
                {
                    case 0x00:
                        break;

                    case 0x81:
                        sb.Append("ü");
                        break;

                    case 0x9a:
                        sb.Append("Ü");
                        break;

                    case 0x84:
                        sb.Append("ä");
                        break;

                    case 0x8e:
                        sb.Append("Ä");
                        break;

                    case 0x94:
                        sb.Append("ö");
                        break;

                    case 0x99:
                        sb.Append("Ö");
                        break;

                    case 0xe1:
                        sb.Append("ß");
                        break;

                    case 0x1b:
                        br.ReadBytes(3);
                        sb.Append("###");
                        break;

                    default:
                        sb.Append(Encoding.ASCII.GetString(new[] { b }));
                        break;
                }
            }
            while (b != 0x00 && br.BaseStream.Position < br.BaseStream.Length);

            return sb.ToString();
        }

        [DllImport("OpenShadows.Native.dll", EntryPoint = "unpack_bopa", CallingConvention = CallingConvention.StdCall)]
        public static extern int UnpackBoPa([In][Out] byte[] inData, uint inSize, [In][Out] byte[] outData, uint outSize);
    }
}
