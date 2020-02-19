using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace CometChar
{
    public static class Core
    {
        public static async Task<byte[]> GetSegment04Bytes(string rom)
        {
            byte[] seg04load = new byte[4];
            byte[] seg04offsets = new byte[8];
            using (FileStream fs = new FileStream(rom, FileMode.Open, FileAccess.Read))
            {
                await Task.Run(() => fs.Seek(0x2ABCA0, SeekOrigin.Begin));
                await fs.ReadAsync(seg04load, 0, 4);
                //await Task.Run(() => fs.Seek(0x4, SeekOrigin.Current));
                await fs.ReadAsync(seg04offsets, 0, 8);
            }
            if (seg04load[3] != 0x04)
            {
                return new byte[0];
            }
            else
            {
                return seg04offsets;
            }
        }

        public static async Task<long> GetSegment04Offset(string rom)
        {
            byte[] seg04load = await GetSegment04Bytes(rom);
            byte[] seg04offset = seg04load.Take(4).ToArray();
            Array.Reverse(seg04offset);

            return await Task.Run(() => BitConverter.ToUInt32(seg04offset, 0));
        }
        public static async Task<long> GetSegment04Length(string rom)
        {
            uint start, end = 0;
            byte[] seg04load = await GetSegment04Bytes(rom);
            byte[] seg04offset = seg04load.Take(4).ToArray();
            byte[] seg04end = seg04load.Skip(4).ToArray();
            Array.Reverse(seg04offset);
            Array.Reverse(seg04end);
            start = await Task.Run(() => BitConverter.ToUInt32(seg04offset, 0));
            end = await Task.Run(() => BitConverter.ToUInt32(seg04end, 0));
            return end - start;
        }
    }
}
