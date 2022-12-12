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
            while (b != 0x00);

            return sb.ToString();
        }

        [DllImport("OpenShadows.Native.dll", EntryPoint = "unpack_bopa", CallingConvention = CallingConvention.StdCall)]
        public static extern int UnpackBoPa([In][Out] byte[] inData, uint inSize, [In][Out] byte[] outData, uint outSize);
    }
}
