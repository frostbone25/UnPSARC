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
    public class PlaystationArchive
    {
        public static readonly byte[] OodleLzaMagic = { 0x8C, 0x06 };
        public static readonly byte[] ZLibNoMagic = { 0x78, 0x01 };
        public static readonly byte[] ZLibDefaultMagic = { 0x78, 0x9C };
        public static readonly byte[] ZLibBestMagic = { 0x78, 0xDA };

        //||||||||||||||||||||||||||||| HEADER |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| HEADER |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| HEADER |||||||||||||||||||||||||||||

        /// <summary>
        /// [4 Bytes] Offset 0 - Always "PSAR"
        /// </summary>
        public static string Magic { get; set; }

        /// <summary>
        /// [2 Bytes] Offset 4
        /// </summary>
        public short MajorVersion { get; set; }

        /// <summary>
        /// [2 Bytes] Offset 6
        /// </summary>
        public short MinorVersion { get; set; }

        /// <summary>
        /// [4 Bytes] Offset 8
        /// </summary>
        public string CompressionType { get; set; }

        /// <summary>
        /// [4 Bytes] Offset 12
        /// </summary>
        public int DatasStartOffset { get; set; }

        /// <summary>
        /// [4 Bytes] Offset 16
        /// </summary>
        public int FileTableEntrySize { get; set; }

        /// <summary>
        /// [4 Bytes] Offset 20
        /// </summary>
        public int FilesCount { get; set; }

        /// <summary>
        /// [4 Bytes] Offset 24
        /// </summary>
        public int ChunkSize { get; set; }

        /// <summary>
        /// [4 Bytes] Offset 28 - Always 0
        /// </summary>
        public int Unknown1 { get; set; }

        //||||||||||||||||||||||||||||| CALCULATED (NOT SERIALIZED IN FILE) |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| CALCULATED (NOT SERIALIZED IN FILE) |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| CALCULATED (NOT SERIALIZED IN FILE) |||||||||||||||||||||||||||||

        public PlaystationArchiveFile[] Files { get; set; }

        public string[] FileNames { get; set; }

        public int FileTableEntryOffset { get; set; }

        public int ZTableOffset { get; set; }

        public static bool CheckMagic(string value)
        {
            return value == BitConverter.ToString(OodleLzaMagic)
                || value == BitConverter.ToString(ZLibNoMagic)
                || value == BitConverter.ToString(ZLibDefaultMagic)
                || value == BitConverter.ToString(ZLibBestMagic);
        }

        public PlaystationArchive(Stream reader, string outputFolder)
        {
            //serialized header
            Magic = reader.ReadString(4);                           //[4 bytes] - always PSAR
            MajorVersion = reader.ReadValueS16(Endian.Big);         //[2 bytes]
            MinorVersion = reader.ReadValueS16(Endian.Big);         //[2 bytes]
            CompressionType = reader.ReadString(4);                 //[4 bytes]
            DatasStartOffset = reader.ReadValueS32(Endian.Big);     //[4 bytes]
            FileTableEntrySize = reader.ReadValueS32(Endian.Big);   //[4 bytes] - always 30
            FilesCount = reader.ReadValueS32(Endian.Big);           //[4 bytes]
            ChunkSize = reader.ReadValueS32(Endian.Big);            //[4 bytes]
            Unknown1 = reader.ReadValueS32(Endian.Big);             //[4 bytes] - always 0

            //Calculate
            FileTableEntryOffset = 0x20; //32
            ZTableOffset = (FilesCount * FileTableEntrySize) + FileTableEntryOffset; //ZTable is after Files Entry

            //build table
            Files = new PlaystationArchiveFile[FilesCount];

            //loop through each file entry table element
            for (int i = 0; i < Files.Length; i++)
            {
                //seek to the next entry offset
                reader.Seek(FileTableEntryOffset, SeekOrigin.Begin);

                //parse the entry
                Files[i] = new PlaystationArchiveFile(reader, this, outputFolder, i);

                //increment the offset position
                FileTableEntryOffset += FileTableEntrySize;
            }
        }
    }
}
