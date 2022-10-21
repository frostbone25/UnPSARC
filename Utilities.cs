using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gibbed.IO;
using OodleSharp;
using System.IO;

namespace UnPSARC
{
    public static class Utilities
    {
        public static int ReadZSize(Stream Reader)
        {
            return MakeNum(new byte[] { 00, 00, (byte)Reader.ReadByte(), (byte)Reader.ReadByte() });
        }
        public static byte[] ReadAtOffset(this Stream s, long Offset, int Size)
        {
            long pos = s.Position;

            s.Seek(Offset, SeekOrigin.Begin);

            byte[] log = s.ReadBytes(Size);

            s.Seek(pos, SeekOrigin.Begin);

            return log;
        }
        public static byte[] ReadAtOffset(Stream s, long Offset, int size, int ZSize, string CompressionType)
        {
            long pos = s.Position;

            s.Seek(Offset, SeekOrigin.Begin);

            byte[] Block = s.ReadBytes(ZSize);
            byte[] log = { };

            if (CompressionType == "oodl") 
                log = Oodle.Decompress(Block, size);

            if (CompressionType == "zlib") 
                log = Zlib.Decompress(Block, size);

            if (log.Length == 0) 
                log = Block;

            s.Seek(pos, SeekOrigin.Begin);

            return log;
        }
        public static byte[] StreamToByteArray(Stream a)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                a.Position = 0;
                a.CopyTo(ms);

                return ms.ToArray();
            }
        }
        public static int MakeNum(byte[] a)
        {
            Array.Reverse(a);
            return BitConverter.ToInt32(a, 0);
        }
    }
}
