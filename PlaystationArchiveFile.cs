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
    public class PlaystationArchiveFile
    {
        //||||||||||||||||||||||||||||| FILE TABLE ENTRY |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| FILE TABLE ENTRY |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| FILE TABLE ENTRY |||||||||||||||||||||||||||||

        /// <summary>
        /// [16 Bytes] Maybe Hash Names?
        /// </summary>
        public byte[] Unknown1 { get; set; }

        /// <summary>
        /// [4 Bytes] Index Of ZSize In ZSizeTable
        /// </summary>
        public int ZSizeIndex { get; set; }

        /// <summary>
        /// [1 Byte]
        /// </summary>
        public byte Unknown2 { get; set; }

        /// <summary>
        /// [4 Bytes] Real Size of file after decompression
        /// </summary>
        public int UncompressedSize { get; set; }

        /// <summary>
        /// [1 Byte]
        /// </summary>
        public byte Unknown3 { get; set; }

        /// <summary>
        /// [4 Bytes] File data offset start
        /// </summary>
        public long FileDataOffset { get; set; }

        //||||||||||||||||||||||||||||| CALCULATED (NOT SERIALIZED IN FILE) |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| CALCULATED (NOT SERIALIZED IN FILE) |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| CALCULATED (NOT SERIALIZED IN FILE) |||||||||||||||||||||||||||||

        public int ZEntryOffset { get; set; }

        public int RemainingSize { get; set; }

        //||||||||||||||||||||||||||||| FILE DATA |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| FILE DATA |||||||||||||||||||||||||||||
        //||||||||||||||||||||||||||||| FILE DATA |||||||||||||||||||||||||||||

        private Stream MEMORY_FILE { get; set; }

        public byte[] FileData { get; set; }

        public string FileName { get; set; }

        public PlaystationArchiveFile(Stream reader, PlaystationArchive archive, string outputFolder, int index)
        {
            //serialized table entry
            Unknown1 = reader.ReadBytes(0x10);                  //[16 bytes] Maybe Hash Names
            ZSizeIndex = reader.ReadValueS32(Endian.Big);       //[4 bytes] Index Of ZSize In ZSizeTable
            Unknown2 = (byte)reader.ReadByte();                 //[1 byte] A Single Byte
            UncompressedSize = reader.ReadValueS32(Endian.Big); //[4 bytes] Real Size of file after decompression
            Unknown3 = (byte)reader.ReadByte();                 //[1 byte] A Single Byte
            FileDataOffset = reader.ReadValueU32(Endian.Big);   //[4 bytes] File data offset start

            //Some Files have 0 size so better to ignore them!
            if (UncompressedSize == 0)
                return;

            //calculated
            ZEntryOffset = (ZSizeIndex * 2) + archive.ZTableOffset; //Offset Of ZTable Of this Entry
            RemainingSize = UncompressedSize;                       //this will help us in multi chunked buffers

            ParseFileData(reader, archive, outputFolder, index);
        }

        public void ParseFileData(Stream reader, PlaystationArchive archive, string outputFolder, int index)
        {
            reader.Seek(FileDataOffset, SeekOrigin.Begin);
            string Magic = BitConverter.ToString(reader.ReadBytes(2));

            MEMORY_FILE = new MemoryStream();

            //Check if file is compressed or not
            if (PlaystationArchive.CheckMagic(Magic))
            {
                while (true)
                {
                    reader.Seek(ZEntryOffset, SeekOrigin.Begin);

                    int ZSize = Utilities.ReadZSize(reader);

                    if (ZSize == 0)
                        ZSize = archive.ChunkSize;

                    //Amount of ZSIZE data remaining in final block of this file
                    if (RemainingSize < archive.ChunkSize || ZSize == archive.ChunkSize)
                        MEMORY_FILE.WriteBytes(reader.ReadAtOffset(FileDataOffset, RemainingSize, ZSize, archive.CompressionType));
                    else
                        MEMORY_FILE.WriteBytes(reader.ReadAtOffset(FileDataOffset, archive.ChunkSize, ZSize, archive.CompressionType));

                    if (MEMORY_FILE.Length == UncompressedSize)
                    {
                        if (index == 0)
                        {
                            byte[] data = Utilities.StreamToByteArray(MEMORY_FILE);
                            string dataString = Encoding.UTF8.GetString(data);
                            archive.FileNames = dataString.Split(new[] { "\n", "\0" }, StringSplitOptions.None); //split string using new line and null characters.
                            break;
                        }
                        else
                        {
                            FileName = archive.FileNames[index - 1];
                            FileData = Utilities.StreamToByteArray(MEMORY_FILE);
                            MEMORY_FILE.Close();
                            break;
                        }
                    }

                    ZEntryOffset += 2;
                    FileDataOffset += (uint)ZSize;
                    RemainingSize -= archive.ChunkSize;
                }
            }
            //File isn't compressed with oodle lza
            else
            {
                MEMORY_FILE.WriteBytes(Utilities.ReadAtOffset(reader, FileDataOffset, UncompressedSize));

                if (index == 0)
                {
                    byte[] data = Utilities.StreamToByteArray(MEMORY_FILE);
                    string dataString = Encoding.UTF8.GetString(data);
                    archive.FileNames = dataString.Split(new[] { "\n", "\0" }, StringSplitOptions.None); //split string using new line and null characters.
                }
                else
                {
                    FileName = archive.FileNames[index - 1];
                    FileData = Utilities.StreamToByteArray(MEMORY_FILE);
                }
            }
        }

        public void WriteFileToDisk(string outputFolder)
        {
            string extractedFilePath = Path.GetDirectoryName(Path.Combine(outputFolder, FileName));

            if (!Directory.Exists(extractedFilePath))
                Directory.CreateDirectory(extractedFilePath);

            File.WriteAllBytes(Path.Combine(outputFolder, FileName), FileData);

            Console.WriteLine(FileName + " Exported...");
        }
    }
}
