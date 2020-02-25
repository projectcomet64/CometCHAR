using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CometChar.Core;
using CometChar.Structs;

namespace CometChar
{
    public static class Geolayout
    {
        public static uint GetCommandLength(byte[] data)
        {
            switch (data[0])
            {
                // Case for 0x1D: If MSBit is 1, then length is 0xC.
                // Otherwise 8
                case 0x1D:
                    if ((data[1] & 0b10000000) == 0b10000000)
                    {
                        return 0xC;
                    }
                    else
                    {
                        return 8;
                    }
                // Case for 0xA: If LSBit is 1, then length is 0xC.
                // Otherwise 8
                case 0xA:

                    if ((data[1] & 1) == 1)
                    {
                        return 0xC;
                    }
                    else
                    {
                        return 8;
                    }
                // Cases for 0x11, 0x12, and 0x14: If second byte is 8, then length is 0xC.
                // Otherwise 8
                case 0x11:
                case 0x12:
                case 0x14:
                    if ((data[1] & 8) == 8)
                    {
                        return 0xC;
                    }
                    else
                    {
                        return 8;
                    }
                // 0xF is 0x14 bytes long.
                case 0xf:
                    return 0x14;
                // 0x10 is 0x10 bytes long.
                // lol
                case 0x10:
                    return 0x10;
                // 0x8, 0x13 and 0x1C are 0xC bytes long.
                case 0x8:
                case 0x13:
                case 0x1C:
                    return 0xC;
                // Most commands are 8 bytes long.
                case 0x0:
                case 0x2:
                case 0xE:
                case 0x15:
                case 0x16:
                case 0x18:
                case 0x19:
                case 0x1A:
                case 0x1E:
                case 0x1F:
                    return 8;
                // The rest of them are 4 bytes long.
                case 0x1:
                case 0x3:
                case 0x4:
                case 0x5:
                case 0x9:
                case 0xB:
                case 0xC:
                case 0xD:
                case 0x17:
                case 0x20:
                    return 4;
            }
            throw new Exception("Something went wrong while reading the Geo Layout. Unhandled command 0x" + data[0]);
        }

        public static async Task<GeoLayoutInformation> GetGeoLayoutLength(string rompath)
        {
            using (FileStream fs = new FileStream(rompath, FileMode.Open, FileAccess.Read))
            {
                //Read the load command from the segments table
                byte[] loadcmd = new byte[8];
                fs.Seek(0x2ABCE0, SeekOrigin.Begin);
                await fs.ReadAsync(loadcmd, 0, 8);
                long offset = Task.Run(() => GetSegmentOffset(rompath, loadcmd[4])).Result;
                byte[] segoffset = loadcmd.Skip(4).ToArray();
                segoffset = segoffset.Reverse().ToArray();
                byte command = 0;
                //Initial position: the offset at where it is in its segment
                int position = BitConverter.ToInt32(segoffset, 0) & 0x00FFFFFF;
                int oldposition = position;
                int startmargin = position;
                //We're gonna store the address we gotta jump back from here
                Stack<int> ra = new Stack<int>();
                while (command != 01)
                {
                    byte[] geocmd = new byte[4];
                    //Gotta restart from position
                    fs.Seek(offset + position, SeekOrigin.Begin);
                    await fs.ReadAsync(geocmd, 0, 4);
                    int length;

                    // Read length of the command we're reading right now
                    length = (int)GetCommandLength(geocmd);

                    //Uh, is this good practice?
                    fs.Seek(offset + position, SeekOrigin.Begin);
                    byte[] fullgeocmd = new byte[length];
                    fs.Read(fullgeocmd, 0, length);

                    position += length;

                    //What will happen at specific nodes (Jumps and tabs)
                    switch (fullgeocmd[0])
                    {
                        case 0x1:
                            // Position + length is where the geolayout ends.
                            // We will return the difference between beginning and where we found the end.
                            GeoLayoutInformation glInfo = new GeoLayoutInformation
                            {
                                Length = position - startmargin,
                                StartMargin = offset + startmargin
                            };
                            return glInfo;
                        case 0x3:
                            position = ra.Pop();
                            break;
                        case 0x2:
                            // Some jumps don't return, but this may break a character mod
                            // Regardless, it's added for the sake of completeness
                            // Thanks, Kure

                            if (fullgeocmd[1] == 1)
                            {
                                ra.Push(position);
                                position = BitConverter.ToInt32(fullgeocmd.Skip(4).Reverse().ToArray(), 0) & 0x00FFFFFF;

                                // Another thing, we're constantly checking where does the actual data start
                                // because it might be the case where it starts before the actual place
                                // where everything is loaded in something like a huge SWITCH (0x0E).

                                if (position < startmargin)
                                {
                                    startmargin = position;
                                }
                            }
                            break;
                    }
                    //Are we still not done yet?
                    //We are only done when we find a 01 command (End of Geo Layout)
                    command = fullgeocmd[0];
                }
            }
            throw new Exception("Something happened while figuring out the information for the geolayout's length.");
        }
    }
}
