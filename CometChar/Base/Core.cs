using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace CometChar
{
    public static class Core
    {
        /// <summary>
        /// Gets the start and end bytes for a given Segment.
        /// </summary>
        /// <param name="rom">ROM Stream</param>
        /// <param name="segment">Segment number (Defaults to 4)</param>
        /// <returns>A 8 byte array with the offsets for the given Segment.</returns>
        public static async Task<byte[]> GetSegmentBytes(Stream rom, int segment = 4)
        {
            long position;
            uint seg;
            switch (segment)
            {
                case 0x17:
                    position = 0x2ABCB8;
                    seg = 0x17;
                    break;
                case 0x04:
                default:
                    position = 0x2ABCA0;
                    seg = 0x04;
                    break;
            }
            byte[] segload = new byte[4];
            byte[] segoffsets = new byte[8];
            await Task.Run(() => rom.Seek(position, SeekOrigin.Begin));
            await rom.ReadAsync(segload, 0, 4);
            //await Task.Run(() => fs.Seek(0x4, SeekOrigin.Current));
            await rom.ReadAsync(segoffsets, 0, 8);
            if (segload[3] != seg)
            {
                return new byte[0];
            }
            else
            {
                return segoffsets;
            }
        }

        /// <summary>
        /// Gets the start and end bytes for a given Segment. For compatibility purposes.
        /// </summary>
        /// <param name="rom">File path to ROM</param>
        /// <param name="segment">Segment number (Defaults to 4)</param>
        /// <returns>A 8 byte array with the offsets for the given Segment.</returns>
        public static async Task<byte[]> GetSegmentBytes(string rom, int segment = 4)
        {
            using (FileStream fs = new FileStream(rom, FileMode.Open, FileAccess.Read))
            {
                return await GetSegmentBytes(fs, segment);
            }
        }

        /// <summary>
        /// Gets the ROM offset in bytes at which the segment starts. For compatibility purposes.
        /// </summary>
        /// <param name="rom">Filepath to ROM</param>
        /// <param name="segment">Segment number (defaults to 4)</param>
        /// <returns>Offset in bytes in the stream at which the segment starts.</returns>
        public static async Task<long> GetSegmentOffset(string rom, int segment = 4)
        {
            using (FileStream fs = new FileStream(rom, FileMode.Open, FileAccess.Read))
            {
                return await GetSegmentOffset(fs, segment);
            }
        }

        /// <summary>
        /// Gets the ROM offset in bytes at which the segment starts.
        /// </summary>
        /// <param name="rom">ROM Stream</param>
        /// <param name="segment">Segment number (defaults to 4)</param>
        /// <returns>Offset in bytes in the stream at which the segment starts.</returns>
        public static async Task<long> GetSegmentOffset(Stream rom, int segment = 4)
        {
            byte[] segload = await GetSegmentBytes(rom, segment);
            byte[] segoffset = segload.Take(4).ToArray();
            Array.Reverse(segoffset);

            return await Task.Run(() => BitConverter.ToUInt32(segoffset, 0));
        }

        /// <summary>
        /// Gets the total length of a Segment. For compatibility purposes.
        /// </summary>
        /// <param name="rom">Filepath to ROM</param>
        /// <param name="segment">Segment number (defaults to 4)</param>
        /// <returns>Size of the segment in bytes.</returns>
        public static async Task<long> GetSegmentLength(string rom, int segment = 4)
        {
            using (FileStream fs = new FileStream(rom, FileMode.Open, FileAccess.Read))
            {
                return await GetSegmentLength(fs, segment);
            }
        }

        /// <summary>
        /// Gets the total length of a Segment.
        /// </summary>
        /// <param name="rom">ROM Stream</param>
        /// <param name="segment">Segment number (defaults to 4)</param>
        /// <returns>Size of the segment in bytes.</returns>
        public static async Task<long> GetSegmentLength(Stream rom, int segment = 4)
        {
            uint start, end = 0;
            byte[] segload = await GetSegmentBytes(rom, segment);
            byte[] segoffset = segload.Take(4).ToArray();
            byte[] segend = segload.Skip(4).ToArray();
            Array.Reverse(segoffset);
            Array.Reverse(segend);
            start = await Task.Run(() => BitConverter.ToUInt32(segoffset, 0));
            end = await Task.Run(() => BitConverter.ToUInt32(segend, 0));
            return end - start;
        }
    }
}
