using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NesEmu.Memory;
using NesEmu.BUS;
using NesEmu.Exceptions;
using NesEmu.Cartridges;
using Moq;

namespace NesEmuTests.Bus
{
    public class NesBusTests
    {
        #region CPU RAM & Mirroring (0x0000 - 0x1FFF)

        [Fact]
        public void Read_CpuRam_BaseAddress_ShouldReturnCorrectValue()
        {
            // Arrange
            var mem = new NesMemory();
            var bus = new NesBus(mem);
            mem.Write(0x0010, 0x42); // Simula um setup prévio do teste
            
            // Re-instanciamos o NesBus para ele copiar a NesMemory inicial (como no seu construtor)
            bus = new NesBus(mem); 

            // Act
            var result = bus.Read(0x0010);

            // Assert
            Assert.Equal(0x42, result);
        }

        [Fact]
        public void Write_CpuRam_ShouldUpdateCpuVramAndNesMemory()
        {
            // Arrange
            var mem = new NesMemory();
            var bus = new NesBus(mem);

            // Act
            bus.Write(0x0020, 0x99);

            // Assert
            Assert.Equal(0x99, bus.Read(0x0020)); // Lê pelo NesBus
            Assert.Equal(0x99, mem.Read(0x0020)); // Garante que o SyncMemoryForTests funcionou
        }

        [Theory]
        [InlineData(0x0010, 0x0810)] // Espelho 1
        [InlineData(0x0010, 0x1010)] // Espelho 2
        [InlineData(0x0010, 0x1810)] // Espelho 3
        public void Write_CpuRam_BaseAddress_ShouldBeReadableFromMirrors(ushort baseAddr, ushort mirrorAddr)
        {
            // Arrange
            var mem = new NesMemory();
            var bus = new NesBus(mem);

            // Act
            bus.Write(baseAddr, 0xAA);

            // Assert
            Assert.Equal(0xAA, bus.Read(mirrorAddr));
        }

        [Theory]
        [InlineData(0x0815, 0x0015)] // Escreve no Espelho 1, lê na Base
        [InlineData(0x1FFE, 0x07FE)] // Escreve no fim do Espelho 3, lê no fim da Base
        public void Write_CpuRam_MirrorAddress_ShouldUpdateBaseAddress(ushort mirrorAddr, ushort baseAddr)
        {
            // Arrange
            var mem = new NesMemory();
            var bus = new NesBus(mem);

            // Act
            bus.Write(mirrorAddr, 0xBB);

            // Assert
            Assert.Equal(0xBB, bus.Read(baseAddr));
            
            // Verifica se o SyncMemoryForTests sincronizou o endereço base na NesMemory
            Assert.Equal(0xBB, mem.Read(baseAddr)); 
        }

        #endregion

        #region PRG ROM (0x8000 - 0xFFFF) - Test Mode

        [Fact]
        public void Write_PrgRom_TestMode_WriteAllowed_ShouldWriteToNesMemory()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ALLOW_WRITE_ROM", "true");
            try
            {
                var mem = new NesMemory();
                var bus = new NesBus(mem); // Modo de Teste (ROM == null)

                // Act
                bus.Write(0x8000, 0x77);

                // Assert
                Assert.Equal(0x77, bus.Read(0x8000));
                Assert.Equal(0x77, mem.Read(0x8000));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ALLOW_WRITE_ROM", null); // Cleanup
            }
        }

        [Fact]
        public void Write_PrgRom_TestMode_WriteNotAllowed_ShouldThrowException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ALLOW_WRITE_ROM", "false");
            try
            {
                var mem = new NesMemory();
                var bus = new NesBus(mem);

                // Act & Assert
                Assert.Throws<InvalidAddressException>(() => bus.Write(0x8000, 0x77));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ALLOW_WRITE_ROM", null);
            }
        }

        [Fact]
        public void Read_PrgRom_TestMode_ShouldReadDirectlyFromNesMemory()
        {
            // Arrange
            var mem = new NesMemory();
            mem.Write(0xC000, 0x33);
            var bus = new NesBus(mem); // ROM == null

            // Act
            var result = bus.Read(0xC000);

            // Assert
            Assert.Equal(0x33, result);
        }

        #endregion

        #region PRG ROM (0x8000 - 0xFFFF) - Real Mode (With Cartridge)

        // Mock auxiliar simples para criar ROMs falsas para os testes
        // Método auxiliar para criar uma ROM válida para os testes
        private ROM CreateDummyRom(int prgSize)
        {
            // O cabeçalho padrão iNES tem 16 bytes.
            // O tamanho total do "arquivo" será 16 + o tamanho do código do jogo.
            int headerSize = 16;
            byte[] rawFile = new byte[headerSize + prgSize];

            // 1. Assinatura do iNES ("NES\x1A") para passar na validação
            rawFile[0] = 0x4E; // 'N'
            rawFile[1] = 0x45; // 'E'
            rawFile[2] = 0x53; // 'S'
            rawFile[3] = 0x1A;

            // 2. Tamanho da PRG ROM (Byte 4 guarda a quantidade de blocos de 16KB)
            // Se prgSize for 16384 (16KB), isso dá 1. Se for 32768 (32KB), dá 2.
            rawFile[4] = (byte)(prgSize / 16384); 
            
            // 3. Tamanho da CHR ROM (Byte 5). Vamos deixar 0 para esse teste de CPU.
            rawFile[5] = 0; 
            
            // 4. Flags (Byte 6 e 7). Tudo zero (sem mapper complexo, sem trainer).
            rawFile[6] = 0; 
            rawFile[7] = 0;

            // 5. Injeta os dados conhecidos na área do código (logo após o cabeçalho)
            if (prgSize > 0) 
            {
                rawFile[headerSize + 0] = 0xAA; // Início do Bank 1
            }
            if (prgSize == 0x8000) 
            {
                rawFile[headerSize + 0x4000] = 0xBB; // Início do Bank 2
            }

            // Agora instanciamos a ROM de verdade passando o array raw válido!
            return new ROM([.. rawFile]);
        }

        [Fact]
        public void Read_PrgRom_RealMode_32KB_ShouldMapWithoutMirroring()
        {
            // Arrange
            var rom = CreateDummyRom(0x8000); // 32KB
            var bus = new NesBus(rom);

            // Act
            var readStart = bus.Read(0x8000); // Deve ler o índice 0 (0xAA)
            var readMiddle = bus.Read(0xC000); // Deve ler o índice 0x4000 (0xBB)

            // Assert
            Assert.Equal(0xAA, readStart);
            Assert.Equal(0xBB, readMiddle);
        }

        [Fact]
        public void Read_PrgRom_RealMode_16KB_ShouldApplyMirroring()
        {
            // Arrange
            var rom = CreateDummyRom(0x4000); // 16KB (Apenas 1 Bank)
            var bus = new NesBus(rom);

            // Act
            var readStart = bus.Read(0x8000); // Mapeia para índice 0
            var readMirrored = bus.Read(0xC000); // Espelho! Deve mapear para o índice 0 também

            // Assert
            Assert.Equal(0xAA, readStart);
            Assert.Equal(0xAA, readMirrored); // Confirma o espelhamento de 16KB
        }

        [Fact]
        public void Write_PrgRom_RealMode_ShouldThrowException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ALLOW_WRITE_ROM", "false");
            try
            {
                var rom = CreateDummyRom(0x4000);
                var bus = new NesBus(rom);

                // Act & Assert
                // Tentar escrever no cartucho deve lançar erro
                Assert.Throws<InvalidAddressException>(() => bus.Write(0x8000, 0x11));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ALLOW_WRITE_ROM", null);
            }
        }

        #endregion

        #region Fallback / Other Memory (0x2000 - 0x7FFF)

        [Fact]
        public void Read_FallbackMemory_ShouldReadFromNesMemory()
        {
            // Arrange
            var mem = new NesMemory();
            mem.Write(0x2000, 0x55); // Ex: PPU Registers
            mem.Write(0x6000, 0x66); // Ex: Save RAM
            var bus = new NesBus(mem);

            // Act & Assert
            Assert.Equal(0x55, bus.Read(0x2000));
            Assert.Equal(0x66, bus.Read(0x6000));
        }

        [Fact]
        public void Write_FallbackMemory_ShouldWriteToNesMemory()
        {
            // Arrange
            var mem = new NesMemory();
            var bus = new NesBus(mem);

            // Act
            bus.Write(0x2000, 0x55);
            bus.Write(0x6000, 0x66);

            // Assert
            Assert.Equal(0x55, mem.Read(0x2000));
            Assert.Equal(0x66, mem.Read(0x6000));
        }

        #endregion

        #region Little Endian Tests

        [Fact]
        public void ReadLittleEndian_ShouldCombineBytesCorrectly()
        {
            // Arrange
            var mem = new NesMemory();
            var bus = new NesBus(mem);
            
            // Byte baixo em 0x0010, Byte alto em 0x0011
            bus.Write(0x0010, 0x34);
            bus.Write(0x0011, 0x12);

            // Act
            ushort result = bus.ReadLittleEndian(0x0010);

            // Assert
            Assert.Equal(0x1234, result);
        }

        #endregion
    }
}
