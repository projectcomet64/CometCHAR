using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace CometChar
{
    public static class Core
    {
        public static async Task<byte[]> GetSegmentBytes(string rom, int segment = 4)
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
            using (FileStream fs = new FileStream(rom, FileMode.Open, FileAccess.Read))
            {
                await Task.Run(() => fs.Seek(position, SeekOrigin.Begin));
                await fs.ReadAsync(segload, 0, 4);
                //await Task.Run(() => fs.Seek(0x4, SeekOrigin.Current));
                await fs.ReadAsync(segoffsets, 0, 8);
            }
            if (segload[3] != seg)
            {
                return new byte[0];
            }
            else
            {
                return segoffsets;
            }
        }

        public static async Task<long> GetSegmentOffset(string rom, int segment = 4)
        {
            byte[] segload = await GetSegmentBytes(rom, segment);
            byte[] segoffset = segload.Take(4).ToArray();
            Array.Reverse(segoffset);

            return await Task.Run(() => BitConverter.ToUInt32(segoffset, 0));
        }
        public static async Task<long> GetSegmentLength(string rom, int segment = 4)
        {
            uint start, end = 0;
            byte[] segload = await GetSegmentBytes(rom);
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
