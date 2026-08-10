using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NesEmu.Exceptions;
using NesEmu.Memory;

namespace NesEmu.BUS
{
    public class Bus(NesMemory nesMemory)
    {
        private readonly NesMemory memory = nesMemory;
        
        public int MemoryLength
        {
            get
            {
                return memory.Length;
            }
        }

        #region Read

        public byte Read(ushort addr)
        {
            if (addr < 0x0000)
                throw new InvalidAddressException($"Address cannot be lower than zero");

            if (addr < 0x1FFF)
                return ReadCpuRam(addr);

            return memory.Read(addr);
            
            throw new InvalidAddressException("Value cannot be greater than 0xFFFF");
        }

        public ushort ReadLittleEndian(ushort addr)
        {
            return memory.ReadLittleEndian(addr);
        }


        public byte ReadCpuRam(ushort addr)
        {
            ushort mirroredAddress = (ushort)(addr & 0x07FF);
            return memory.Read(mirroredAddress);
        }
        #endregion


        #region Write
        public void Write(ushort addr, byte value)
        {
            if (addr < 0x0000)
                throw new InvalidAddressException($"Address cannot be lower than zero");

            if (addr < 0x1FFF){
                WriteCpuRam(addr, value);
                return;
            }
            
            memory.Write(addr, value);

            // TODO: remove
            // throw new InvalidAddressException("Value cannot be greater than 0xFFFF");
        }

        private void WriteCpuRam(ushort addr, byte value)
        {
            ushort mirroredAddress = (ushort)(addr & 0x07FF);
            memory.Write(mirroredAddress, value);
        }

        public void WriteLittleEndian(ushort addr, ushort value)
        {
            memory.WriteLittleEndian(addr, value);
        }
        #endregion

    }


}