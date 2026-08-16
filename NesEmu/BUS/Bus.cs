using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NesEmu.Cartridges;
using NesEmu.Exceptions;
using NesEmu.Memory;

namespace NesEmu.BUS
{
    public class Bus
    {
        // TODO: tentar eliminar essa variável;
        private readonly NesMemory memory;
        private readonly ROM Rom;
        public readonly byte[] CpuVram;

        private readonly bool allowWriteRom;


        public Bus(NesMemory nesMemory)
        {
            memory = nesMemory;
            CpuVram = [.. nesMemory.MemorySpace.Take(2048)];
            allowWriteRom = Environment.GetEnvironmentVariable("ALLOW_WRITE_ROM") == "true";
        }

        public Bus(ROM rom)
        {
            CpuVram = new byte[2048];
            this.Rom = rom;
            allowWriteRom = Environment.GetEnvironmentVariable("ALLOW_WRITE_ROM") == "true";
        }

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

            if (addr <= 0x1FFF)
                return ReadCpuRam(addr);

            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                if (Rom == null) return memory.Read(addr);
                return ReadPrgRom(addr);
            }

            return memory.Read(addr);

            throw new InvalidAddressException("Value cannot be greater than 0xFFFF");
        }

        private byte ReadPrgRom(ushort addr)
        {

            addr -= 0x8000;
            if (Rom.prgRom.Count == 0x4000 && addr >= 0x4000)
            {
                addr = (ushort)(addr % 0x4000);
            }
            return Rom.prgRom[addr];
        }

        public ushort ReadLittleEndian(ushort addr)
        {
            return memory.ReadLittleEndian(addr);
        }


        public byte ReadCpuRam(ushort addr)
        {
            ushort mirroredAddress = (ushort)(addr & 0x07FF);
            return CpuVram[mirroredAddress];
        }
        #endregion


        #region Write
        public void Write(ushort addr, byte value)
        {
            if (addr < 0x0000)
                throw new InvalidAddressException($"Address cannot be lower than zero");

            if (addr < 0x1FFF)
            {
                WriteCpuRam(addr, value);
                return;
            }

            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                if (allowWriteRom)
                {
                    Rom?.prgRom[addr - 0x8000] = value;
                    memory.Write(addr, value);
                    return;
                }

                throw new InvalidAddressException("Cannot write PrgRom cause it's Read Only");
            }

            memory.Write(addr, value);

            // TODO: remove
            // throw new InvalidAddressException("Value cannot be greater than 0xFFFF");
        }

        private void WriteCpuRam(ushort addr, byte value)
        {
            ushort mirroredAddress = (ushort)(addr & 0x07FF);
            CpuVram[mirroredAddress] = value;
            SyncMemoryForTests(addr, mirroredAddress, value);
        }

        public void WriteLittleEndian(ushort addr, ushort value)
        {
            memory.WriteLittleEndian(addr, value);
        }

        /// <summary>
        /// Sincroniza o objeto do tipo NesMemory e o array da variável CpuVram. Isso tornou-se necessário devido ao fator de que 
        /// os testes das instruções da CPU foram escritos antes da implementação do Bus, que separa os diferentes componentes
        /// endereçáveis. Essa função faz com que as escritas no CpuVram sejam também feitas na variável NesMemory,
        /// para evitar que os assert dos testes (que fazem referência direta ao objeto do tipo NesMemory) passem.
        /// </summary>
        /// <param name="originalAddr"></param>
        /// <param name="mirroredAddr"></param>
        /// <param name="value"></param>
        [Conditional("DEBUG")]
        private void SyncMemoryForTests(ushort originalAddr, ushort mirroredAddr, byte value)
        {
            if (memory is null) return;
            memory.Write(mirroredAddr, value);
            memory.Write(originalAddr, value);
        }

        #endregion

    }


}