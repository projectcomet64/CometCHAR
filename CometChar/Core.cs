using System;
using System.IO;
using System.Threading.Tasks;

namespace CometChar
{
    public static class Core
    {
        public static async Task<long> GetSegment04Offset(string rom)
        {
            byte[] seg04load = new byte[12];
            using (FileStream fs = new FileStream(rom, FileMode.Open, FileAccess.Read))
            {
                await Task.Run(() => fs.Read(seg04load, 0x2ABCA0, 120));
            }
            if (seg04load[4] != 0x04)
            {
                return 0;
            }
            else
            {
                return await Task.Run(() => BitConverter.ToInt64(seg04load, 4));
            }
        }
    }
}
