using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CometChar
{
    class InvalidROMException : Exception
    {
        public InvalidROMException()
        {
        }

        public InvalidROMException(string message)
            : base(message)
        {
        }

        public InvalidROMException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
