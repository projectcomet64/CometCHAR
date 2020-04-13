using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using static CometChar.Core;
using static CometChar.Geolayout;
using CometChar.Structs;
using Encoding.SevenZip;
using SevenZip;
using static System.Text.Encoding;

namespace CometChar
{
    public static class Patch
    {
        public static PatchInformation ReadPatchFile(Stream PatchStream)
        {
            PatchInformation pI = new PatchInformation();
            byte[] cmtHeader = new byte[8];
            byte[] magicNumber = new byte[4];
            byte[] versionMajor = new byte[1];
            byte[] versionMinor = new byte[1];
            byte[] features = new byte[2];
            PatchStream.Read(magicNumber, 0, 4);
            PatchStream.Read(versionMajor, 0, 1);
            PatchStream.Read(versionMinor, 0, 1);
            PatchStream.Read(features, 0, 2);
            if (ASCII.GetString(magicNumber) != "CMTP")
            {
                throw new ArgumentException("File provided is not a valid CometCHAR Patch.");
            }
            byte[] lenCompressed04 = new byte[4];
            byte[] lenCompressedGL = new byte[4];
            byte[] CRC = new byte[4];
            byte[] len04 = new byte[4];
            byte[] lenGL = new byte[4];
            byte[] segAddr = new byte[4];

            PatchStream.Read(lenCompressed04, 0, 4);
            PatchStream.Read(lenCompressedGL, 0, 4);
            PatchStream.Read(CRC, 0, 4);
            PatchStream.Read(len04, 0, 4);
            PatchStream.Read(lenGL, 0, 4);
            PatchStream.Read(segAddr, 0, 4);

            pI.versionMajor = versionMajor[0];
            pI.versionMinor = versionMinor[0];
            pI.Features = BitConverter.ToUInt16(features, 0);
            pI.compSegment04Length = BitConverter.ToUInt32(lenCompressed04, 0);
            pI.compGeoLayoutLength = BitConverter.ToUInt32(lenCompressedGL, 0);
            pI.CompressedCRC = BitConverter.ToUInt32(CRC, 0);
            pI.Segment04Length = BitConverter.ToUInt32(len04, 0);
            pI.GeoLayoutLength = BitConverter.ToUInt32(lenGL, 0);
            pI.GeoLayoutSegOffset = BitConverter.ToUInt32(segAddr, 0);
            return pI;
        }

        public static void CreatePatchFile(Stream ROMStream, string outFile)
        {
            bool GeoLayoutInSeg04 = false;
            using (FileStream fs = new FileStream(outFile, FileMode.Create, FileAccess.ReadWrite))
            {
                long Seg04Offset = Task.Run(() => GetSegmentOffset(ROMStream)).Result;
                long Seg04Length = Task.Run(() => GetSegmentLength(ROMStream)).Result;
                GeoLayoutInformation GeoInfo = Task.Run(() => GetGeoLayoutLength(ROMStream)).Result;
                byte[] GeoLayoutData = new byte[GeoInfo.Length];
                byte[] Seg04Data = new byte[Seg04Length];


                ROMStream.Seek(Seg04Offset, SeekOrigin.Begin);
                ROMStream.Read(Seg04Data, 0, (int)Seg04Length);

                if (GeoInfo.StartMargin > Seg04Offset && GeoInfo.StartMargin < Seg04Offset + Seg04Length)
                {
                    GeoLayoutInSeg04 = true;
                }
                else
                {
                    ROMStream.Seek(GeoInfo.StartMargin, SeekOrigin.Begin);
                    ROMStream.Read(GeoLayoutData, 0, (int)GeoInfo.Length);
                }

                MemoryStream compressedS04 = new MemoryStream();
                MemoryStream compressedGL = new MemoryStream();
                uint CRCChecksum;
                byte[] GeoLayoutSegAddr = new byte[8];
                using (MemoryStream ms = new MemoryStream(Seg04Data))
                {
                    LZMA.Compress(ms, compressedS04, LzmaSpeed.Medium, DictionarySize.Medium);
                    ms.Flush();
                }

                if (!GeoLayoutInSeg04)
                {
                    using (MemoryStream ms = new MemoryStream(GeoLayoutData))
                    {
                        LZMA.Compress(ms, compressedGL, LzmaSpeed.Medium, DictionarySize.Medium);
                        ms.Flush();
                    }
                }

                using (MemoryStream tempStream = new MemoryStream((int)(compressedGL.Length + compressedS04.Length)))
                {
                    CRC checksum = new CRC();
                    checksum.Init();
                    tempStream.Write(compressedS04.GetBuffer(), 0, (int)compressedS04.Length);
                    tempStream.Write(compressedGL.GetBuffer(), 0, (int)compressedGL.Length);
                    checksum.Update(tempStream.GetBuffer(), 0, (uint)tempStream.Length);
                    CRCChecksum = checksum.GetDigest();
                    tempStream.Flush();
                }

                ROMStream.Seek(0x2ABCE0, SeekOrigin.Begin);
                ROMStream.Read(GeoLayoutSegAddr, 0, 8);

                // Time to write everything
                fs.Write(ASCII.GetBytes("CMTP".ToCharArray()), 0, 4);
                fs.Write(new byte[] { 00, 01 }, 0, 2);
                fs.Write(new byte[] { 00, 00 }, 0, 2);
                fs.Write(BitConverter.GetBytes((uint)compressedS04.Length), 0, 4);
                fs.Write(BitConverter.GetBytes((uint)compressedGL.Length), 0, 4);
                fs.Write(BitConverter.GetBytes(CRCChecksum), 0, 4);
                fs.Write(BitConverter.GetBytes(Seg04Length), 0, 4);
                fs.Write(BitConverter.GetBytes(GeoInfo.Length), 0, 4);
                fs.Write(GeoLayoutSegAddr.Skip(4).ToArray(), 0, 4);
                fs.Write(compressedS04.GetBuffer(), 0, (int)compressedS04.Length);
                fs.Write(compressedGL.GetBuffer(), 0, (int)compressedGL.Length);
                compressedGL.Flush();
                compressedS04.Flush();
                ROMStream.Dispose();
            }
        }
    }
}
