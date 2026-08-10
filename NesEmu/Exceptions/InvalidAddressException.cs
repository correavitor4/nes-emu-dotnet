using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NesEmu.Exceptions
{
    public class InvalidAddressException : Exception
    {

        public InvalidAddressException(string message) : base(message)
        {
        }

        public InvalidAddressException(string message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}