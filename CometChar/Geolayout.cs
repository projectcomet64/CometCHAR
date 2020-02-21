using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CometChar
{
    public static class Geolayout
    {
        public static uint GetCommandLength(byte[] data, uint position)
        {
            switch (data[position])
            {
                case 0x1D:
                    if ((data[position + 1] & 0b10000000) == 0b10000000)
                    {
                        return 0xC;
                    }
                    else
                    {
                        return 8;
                    }
                case 0xA:

                    if ((data[position + 1] & 1) == 1)
                    {
                        return 0xC;
                    }
                    else
                    {
                        return 8;
                    }
                case 0x11:
                case 0x12:
                case 0x14:
                    if ((data[position + 1] & 8) == 8)
                    {
                        return 0xC;
                    }
                    else
                    {
                        return 8;
                    }
                case 0xf:
                    return 0x14;
                case 0x10:
                    return 0x10;
                case 0x8:
                case 0x13:
                case 0x1C:
                    return 0xC;
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
            throw new Exception("What happened?");
            return 0;
        }
    }
}
