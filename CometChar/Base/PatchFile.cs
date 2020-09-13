using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using static CometChar.Core;
using static CometChar.Geolayout;
using CometChar.Structs;
using Encoding.SevenZip;
using static System.Text.Encoding;

namespace CometChar
{
    public static class Patch
    {
        public static PatchInformation ReadPatchFile(Stream PatchStream)
        {
            PatchInformation pI = new PatchInformation();
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
            byte[] StartMargin = new byte[4];
            byte[] len04 = new byte[4];
            byte[] lenGL = new byte[4];
            byte[] segAddr = new byte[4];

            PatchStream.Read(lenCompressed04, 0, 4);
            PatchStream.Read(lenCompressedGL, 0, 4);
            PatchStream.Read(StartMargin, 0, 4);
            PatchStream.Read(len04, 0, 4);
            PatchStream.Read(lenGL, 0, 4);
            PatchStream.Read(segAddr, 0, 4);

            pI.versionMajor = versionMajor[0];
            pI.versionMinor = versionMinor[0];
            pI.Features = BitConverter.ToUInt16(features, 0);
            pI.compSegment04Length = BitConverter.ToUInt32(lenCompressed04, 0);
            pI.compGeoLayoutLength = BitConverter.ToUInt32(lenCompressedGL, 0);
            pI.GeoLayoutStartMargin = BitConverter.ToUInt32(StartMargin, 0);
            pI.Segment04Length = BitConverter.ToUInt32(len04, 0);
            pI.GeoLayoutLength = BitConverter.ToUInt32(lenGL, 0);
            pI.GeoLayoutSegOffset = BitConverter.ToUInt32(segAddr, 0);
            return pI;
        }

        public static bool CheckROMBigEndian(string inROM)
        {
            using (FileStream romStream = new FileStream(inROM, FileMode.Open, FileAccess.Read))
            {
                byte[] headerArr = new byte[] { 0x80, 0x37, 0x12, 0x40 };
                byte[] readArr = new byte[4];
                romStream.Seek(0x0, SeekOrigin.Begin);
                romStream.Read(readArr, 0, 4);
                if (!Enumerable.SequenceEqual(headerArr, readArr))
                {
                    return false;
                }
                return true;
            }

        }

        public static void PatchROM(string inROM, Stream patchStream, string outROM, IProgress<float> prog)
        {
            PatchInformation pInfo;
            pInfo = ReadPatchFile(patchStream);

            MemoryStream uncompS04 = new MemoryStream((int)pInfo.Segment04Length);
            MemoryStream uncompGL = new MemoryStream((int)pInfo.GeoLayoutLength);

            bool validROM = CheckROMBigEndian(inROM);
            if (!validROM)
            {
                throw new InvalidROMException("This ROM is not a Big Endian (z64) ROM.");
            }

            // Decompressing the compressed data and checking the ROM's byte order
            patchStream.Seek(0x20, SeekOrigin.Begin);
            byte[] s04arr = new byte[pInfo.compSegment04Length];
            patchStream.Read(s04arr, 0, s04arr.Length);
            MemoryStream s04comp = new MemoryStream(s04arr);
            LZMA.Decompress(s04comp, uncompS04);
            prog?.Report(1);
            if ((pInfo.Features & 2) == 0)
            {
                // Hierarchy (GL) not in Bank04, decompress at default location
                byte[] glarr = new byte[pInfo.compGeoLayoutLength];
                patchStream.Read(glarr, 0, glarr.Length);
                MemoryStream glcomp = new MemoryStream(glarr);
                LZMA.Decompress(glcomp, uncompGL);
                glcomp.Dispose();
            }
            s04comp.Dispose();

            prog?.Report(1.6f);
            if (File.Exists(outROM))
            {
                File.Delete(outROM);
            }
            File.Copy(inROM, outROM);
            prog?.Report(2f);

            //Writing to ROM
            using (FileStream fs = new FileStream(outROM, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Seek(0x2ABCE4, SeekOrigin.Begin);
                fs.Write(BitConverter.GetBytes(pInfo.GeoLayoutSegOffset), 0, 4);
                prog?.Report(3f);
                // Extend S04
                if ((pInfo.Features & 1) == 1)
                {
                    fs.Seek(0x2ABCA4, SeekOrigin.Begin);
                    fs.Write(new byte[] { 0x01, 0x1A, 0x35, 0xB8, 0x01, 0x1F, 0xFF, 0x00 }, 0, 8);
                }
                prog?.Report(3.2f);
                fs.Seek(Task.Run(() => GetSegmentOffset(fs, 04)).Result, SeekOrigin.Begin);
                fs.Write(uncompS04.GetBuffer(), 0, (int)uncompS04.Length);
                prog?.Report(3.6f);
                if ((pInfo.Features & 2) == 0)
                {
                    // GL not in Bank 04, write uncompressed GL
                    fs.Seek(pInfo.GeoLayoutStartMargin, SeekOrigin.Begin);
                    fs.Write(uncompGL.GetBuffer(), 0, (int)uncompGL.Length);
                }
                prog?.Report(3.8f);

            }
            uncompGL.Dispose();
            uncompS04.Dispose();
            prog?.Report(4);
        }

        public static void CreatePatchFile(Stream ROMStream, string outFile)
        {
            bool GeoLayoutInSeg04 = false;
            uint features = 0;
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }
            using (FileStream fs = new FileStream(outFile, FileMode.Create, FileAccess.ReadWrite))
            {
                long Seg04Offset = Task.Run(() => GetSegmentOffset(ROMStream)).Result;
                long Seg04Length = Task.Run(() => GetSegmentLength(ROMStream)).Result;
                GeoLayoutInformation GeoInfo = Task.Run(() => GetGeoLayoutLength(ROMStream)).Result;
                byte[] GeoLayoutData = new byte[GeoInfo.Length];
                byte[] Seg04Data = new byte[Seg04Length];


                ROMStream.Seek(Seg04Offset, SeekOrigin.Begin);
                ROMStream.Read(Seg04Data, 0, (int)Seg04Length);

                // Original length of Bank 04 is 0x35378
                if (Seg04Length > 0x35378)
                    features |= 1;

                if (GeoInfo.StartMargin > Seg04Offset && GeoInfo.StartMargin < Seg04Offset + Seg04Length)
                {
                    GeoLayoutInSeg04 = true;
                    features |= 2;
                }
                else
                {
                    ROMStream.Seek(GeoInfo.StartMargin, SeekOrigin.Begin);
                    ROMStream.Read(GeoLayoutData, 0, (int)GeoInfo.Length);
                }

                MemoryStream compressedS04 = new MemoryStream();
                MemoryStream compressedGL = new MemoryStream();
                // TODO: Implement Checksum
                // It'll be hard, can't rely on 7z's since it takes too much mem
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

                ROMStream.Seek(0x2ABCE0, SeekOrigin.Begin);
                ROMStream.Read(GeoLayoutSegAddr, 0, 8);

                // Time to write everything
                fs.Write(ASCII.GetBytes("CMTP".ToCharArray()), 0, 4);
                fs.Write(new byte[] { 00, 01 }, 0, 2); //CMTP v0.1
                fs.Write(BitConverter.GetBytes((ushort)features), 0, 2);
                fs.Write(BitConverter.GetBytes((uint)compressedS04.Length), 0, 4);
                fs.Write(BitConverter.GetBytes((uint)compressedGL.Length), 0, 4);
                fs.Write(BitConverter.GetBytes((uint)GeoInfo.StartMargin), 0, 4);
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
