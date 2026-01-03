using System.Runtime.InteropServices;
using Moq;
using NesEmu.CPU;
using NesEmu.Memory;

namespace NesEmuTests.CPU;

public class InstructionsTests
{
    #region LDA

    [Fact]
    public void TestLDAImmediateAddrMode__WithPositiveValue__ShouldLoadValueAndSetFlags()
    {
        // Arrange
        var program = new byte[10];

        program[0] = 0xA9;
        program[1] = 10;
        program[2] = 0x00; // stop condition
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));

        // Act
        cpu.Interpret();

        // Assert
        Assert.Equal(10, cpu.GetRegisterA());
        AssertStatusRegisterEqualZeroFlag(cpu, false);
        AssertStatusRegisterEqualNegativeFlag(cpu, false);
    }

    [Fact]
    public void TestLDAImmediateAddrMode__WithNegativeValue__ShouldLoadValueAndSetFlags()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = 0b1000_0010;
        program[2] = 0x00;
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));

        // Act
        cpu.Interpret();

        // Assert
        Assert.Equal(0b1000_0010, cpu.GetRegisterA());
        AssertStatusRegisterEqualZeroFlag(cpu, false);
        AssertStatusRegisterEqualNegativeFlag(cpu, true);
    }

    [Fact]
    public void TestLDAImmediateAddrMode__WithZeroValue__ShouldLoadValueAndSetFlags()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = 0;
        program[2] = 0x00;
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));

        // Act
        cpu.Interpret();

        // Assert
        Assert.Equal(0b0000_0000, cpu.GetRegisterA());
        AssertStatusRegisterEqualZeroFlag(cpu, true);
        AssertStatusRegisterEqualNegativeFlag(cpu, false);
    }

    [Theory]
    [InlineData(0xA5, AddressingMode.ZeroPage)]
    [InlineData(0xB5, AddressingMode.ZeroPageX)]
    [InlineData(0xAD, AddressingMode.Absolute)]
    public void TestLDA__DifferentOpcodesLoadCorrectly(byte opcode, AddressingMode _)
    {
        // Arrange
        var program = new byte[0xFFFF];
        program[0] = opcode;
        program[1] = 0x42;
    
        var memory = NesMemory.FromBytesArray(program);
        memory.Write(0x42, 0x55);  // Para todos os modos simples
    
        var cpu = new NesEmu.CPU.CPU(memory);  // CPU REAL!
    
        // Act
        cpu.Interpret(limit: 1);
    
        // Assert - Opcode certo = Lda com modo certo!
        Assert.Equal(0x55, cpu.GetRegisterA());  
        Assert.Equal(2, cpu.ProgramCounter);     // Avançou 2 bytes ✓
    }


    
    
    #endregion

    #region TAX

    [Fact]
    public void Test_TAX__WithPositiveValue__ShouldLoadValueAndSetFlags()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = 0b0000_00101;
        program[2] = 0xAA;
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));

        // Act
        cpu.Interpret();

        //
        Assert.Equal(0b0000_00101, cpu.GetRegisterA());
        AssertStatusRegisterEqualNegativeFlag(cpu, false);
        AssertStatusRegisterEqualZeroFlag(cpu, false);
    }

    [Fact]
    public void Test_TAX__WithNegativeValue__ShouldLoadValueAndSetFlags()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = 0b1000_0101;
        program[2] = 0xAA;
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));

        // Act
        cpu.Interpret();

        //
        Assert.Equal(0b1000_0101, cpu.GetRegisterA());
        AssertStatusRegisterEqualNegativeFlag(cpu, true);
        AssertStatusRegisterEqualZeroFlag(cpu, false);
    }

    [Fact]
    public void Test_TAX__WithZeroValue__ShouldLoadValueAndSetFlags()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = 0;
        program[2] = 0xAA;
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));

        // Act
        cpu.Interpret();

        //
        Assert.Equal(0, cpu.GetRegisterA());
        AssertStatusRegisterEqualNegativeFlag(cpu, false);
        AssertStatusRegisterEqualZeroFlag(cpu, true);
    }

    #endregion

    #region INX

    [Theory]
    [InlineData(0xFF, 0x00, false, true)]
    [InlineData(0x00, 0x01, false, false)]
    [InlineData(0xFD, 0xFE, true, false)]
    [InlineData(0x02, 0x03, false, false)]
    public void TextInx____ShouldIncrementAndSetFlags(byte previousValue, byte expectedFinalValue,
        bool expectedNegativeFlag, bool expectedZeroFlag)
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = previousValue;
        program[2] = 0xAA;
        program[3] = 0xE8;
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));

        // Act
        cpu.Interpret();

        // Assert
        Assert.Equal(expectedFinalValue, cpu.GetRegisterX());
        AssertStatusRegisterEqualNegativeFlag(cpu, expectedNegativeFlag);
        AssertStatusRegisterEqualZeroFlag(cpu, expectedZeroFlag);
    }

    [Fact]
    public void TestInxOverflow____ShouldRegisterXContains01AtTheEnd()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = 0xFF;
        program[2] = 0xAA;
        program[3] = 0xE8;
        program[4] = 0xE8;
        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));


        // Act
        cpu.Interpret();


        // Assert
        Assert.Equal(0x01, cpu.GetRegisterX());
    }

    #endregion

    #region Addressing

    [Fact]
    public void TestGetOperand__WithImmediateMode__ShouldResultProgramCounterItself()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0x0A;
        var memory = NesMemory.FromBytesArray(program);
        var cpu = new NesEmu.CPU.CPU(memory);

        // Act
        var res = cpu.GetOperandAddress(AddressingMode.Immediate);

        // Assert
        Assert.Equal(0x00, res);
    }

    [Fact]
    public void TestGetOperand__WithZeroPageMode__ShouldReturnOperandAddress()
    {
        // Arrange
        var program = new byte[10];
        program[0] = 0xA9;
        program[1] = 0x06;
        program[6] = 0xA7;

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.ProgramCounter = 1;


        // Act
        var res = cpu.GetOperandAddress(AddressingMode.ZeroPage);


        // Assert
        Assert.Equal(0x06, res);
    }


    [Fact]
    public void TestGetOperand__WithZeroAbsoluteMode__ShouldReturnOperandAddress()
    {
        // Arrange
        var program = new byte[1024];
        program[0] = 0xA9;
        program[1] = 0xB1;
        program[2] = 0x02;
        program[689] = 0xA7;

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.ProgramCounter = 1;


        // Act
        var res = cpu.GetOperandAddress(AddressingMode.Absolute);


        // Assert
        Assert.Equal(0x02B1, res);
    }

    [Theory]
    [InlineData(0x10, 0x00, 0x10)]
    [InlineData(0x10, 0x05, 0x15)]
    [InlineData(0xFF, 0x01, 0x00)]
    [InlineData(0xFE, 0x05, 0x03)]
    public void TestGetOperand__WithZeroPageX__ShouldReturnOperandAddress(byte memoryAddress,
        byte registerXInitialValue, byte expectedAddress)
    {
        // Arrange
        var program = new byte[200];
        program[1] = memoryAddress;

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.SetRegisterX(registerXInitialValue);
        cpu.ProgramCounter = 1;


        // Act
        var res = cpu.GetOperandAddress(AddressingMode.ZeroPageX);


        // Assert
        Assert.Equal(res, expectedAddress);
    }

    [Theory]
    [InlineData(0x10, 0x00, 0x10)]
    [InlineData(0x10, 0x05, 0x15)]
    [InlineData(0xFF, 0x01, 0x00)]
    [InlineData(0xFE, 0x05, 0x03)]
    public void TestGetOperand__WithZeroPageY__ShouldReturnOperandAddress(byte memoryAddress,
        byte registerYInitialValue, byte expectedAddress)
    {
        // Arrange
        var program = new byte[200];
        program[1] = memoryAddress;

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.SetRegisterY(registerYInitialValue);
        cpu.ProgramCounter = 1;


        // Act
        var res = cpu.GetOperandAddress(AddressingMode.ZeroPageY);


        // Assert
        Assert.Equal(res, expectedAddress);
    }


    [Theory]
    [InlineData(0x10, 0x00, 0x10)]
    [InlineData(0x10, 0x05, 0x15)]
    [InlineData(0x200, 0x00, 0x200)]
    [InlineData(0x200, 0xC8, 0x2C8)]
    [InlineData(0xFFFF, 0x01, 0x0000)]
    [InlineData(0xFFFD, 0x05, 0x0002)]
    public void TestGetOperand__WithAbsoluteX__ShouldReturnOperandAddress(ushort memoryAddress,
        byte registerXInitialValue, ushort expectedAddress)
    {
        // Arrange
        var program = new byte[200];
        program[1] = (byte)(memoryAddress & 0x00FF);
        program[2] = (byte)((memoryAddress >> 8) & 0x00FF);

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.SetRegisterX(registerXInitialValue);
        cpu.ProgramCounter = 1;


        // Act
        var res = cpu.GetOperandAddress(AddressingMode.AbsoluteX);


        // Assert
        Assert.Equal(res, expectedAddress);
    }

    [Theory]
    [InlineData(0x10, 0x00, 0x10)]
    [InlineData(0x10, 0x05, 0x15)]
    [InlineData(0x200, 0x00, 0x200)]
    [InlineData(0x200, 0xC8, 0x2C8)]
    [InlineData(0xFFFF, 0x01, 0x0000)]
    [InlineData(0xFFFD, 0x05, 0x0002)]
    public void TestGetOperand__WithAbsoluteY__ShouldReturnOperandAddress(ushort memoryAddress,
        byte registerYInitialValue, ushort expectedAddress)
    {
        // Arrange
        var program = new byte[200];
        program[1] = (byte)(memoryAddress & 0x00FF);
        program[2] = (byte)((memoryAddress >> 8) & 0x00FF);

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.SetRegisterY(registerYInitialValue);
        cpu.ProgramCounter = 1;


        // Act
        var res = cpu.GetOperandAddress(AddressingMode.AbsoluteY);


        // Assert
        Assert.Equal(res, expectedAddress);
    }

    [Theory]
    [InlineData(0x40, 0x04, 0x5678)] // base=0x40, X=0x04, ptr=0x44, vetor=0x7856→0x5678
    [InlineData(0xFF, 0x01, 0x5678)] // base=0xFF, X=0x01, ptr=0x00, vetor=0x7856→0x5678
    [InlineData(0xFE, 0x01, 0x5678)] // base=0xFE, X=0x01, ptr=0xFF, vetor=0x7856→0x5678
    public void TestGetOperand__WithIndirectX__ShouldReturnOperandAddress(
        byte baseAddr, byte registerX, ushort expectedFinalAddr)
    {
        // Arrange
        var ptrAddr = (byte)(baseAddr + registerX); // Ex: 40+04=44
        var expectedLo = (byte)(expectedFinalAddr & 0xFF); // LSB: 78
        var expectedHi = (byte)((expectedFinalAddr >> 8) & 0xFF); // MSB: 56

        var program = new byte[0xFFFF];
        program[3] = baseAddr; // PC=1 lê base=40
        program[ptrAddr] = expectedLo; // 44=78
        program[(byte)(ptrAddr + 1)] = expectedHi; // 45=56

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.SetRegisterX(registerX);
        cpu.ProgramCounter = 3;

        // Act
        var result = cpu.GetOperandAddress(AddressingMode.IndirectX);

        // Assert
        Assert.Equal(expectedFinalAddr, result);
    }

    [Theory]
    [InlineData(0x40, 0x04, 0x5678)] // base=0x40, X=0x04, ptr=0x44, vetor=0x7856→0x5678
    [InlineData(0xFF, 0x04, 0x5678)] // base=0x40, X=0x04, ptr=0x44, vetor=0x7856→0x5678
    [InlineData(0x40, 0x04, 0xFFFF)] // base=0x40, X=0x04, ptr=0x44, vetor=0x7856→0x5678
    public void TestGetOperand__WithIndirectY__ShouldReturnOperandAddress(
        byte baseAddr, byte registerY, ushort expectedFinalAddr)
    {
        // Arrange
        var expectedLo = (byte)(expectedFinalAddr & 0xFF); // LSB: 78
        var expectedHi = (byte)((expectedFinalAddr >> 8) & 0xFF); // MSB: 56

        var program = new byte[0xFFFF];
        program[3] = baseAddr; // PC=1 lê base=40
        program[baseAddr] = expectedLo; // 44=78
        program[(byte)(baseAddr + 1)] = expectedHi; // 45=56

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));
        cpu.SetRegisterY(registerY);
        cpu.ProgramCounter = 3;

        // Act
        var result = cpu.GetOperandAddress(AddressingMode.IndirectY);

        // Assert
        Assert.Equal((ushort)(expectedFinalAddr + registerY), result);
    }

    #endregion

    #region LDA, TAX and INX workings together

    [Fact]
    public void TestLdaTaxAndInx__WorkingTogether__ShouldRegisterXBe0xC1()
    {
        // Arrange
        var program = new byte[5];
        program[0] = 0xA9;
        program[1] = 0xC0;
        program[2] = 0xAA;
        program[3] = 0xE8;
        program[4] = 0x00;

        var cpu = new NesEmu.CPU.CPU(NesMemory.FromBytesArray(program));


        // Act
        cpu.Interpret();


        // Assert
        Assert.Equal(0xC1, cpu.GetRegisterX());
    }

    #endregion

    #region Helpers

    private void AssertStatusRegisterEqualZeroFlag(NesEmu.CPU.CPU cpu, bool expected)
    {
        if (expected)
        {
            Assert.Equal(0b0000_0010, cpu.GetRegisterStatus() & 0b0000_0010); // Status register equals zero
            return;
        }

        Assert.Equal(0b0000_0000, cpu.GetRegisterStatus() & 0b0000_0010); // status register not equals zero
    }

    private void AssertStatusRegisterEqualNegativeFlag(NesEmu.CPU.CPU cpu, bool expected)
    {
        if (expected is false)
        {
            Assert.Equal(0b0000_0000, cpu.GetRegisterStatus() & 0b1000_0000); // Status register non negative
            return;
        }

        Assert.Equal(0b1000_0000, cpu.GetRegisterStatus() & 0b1000_0000); // status register equals negative 
    }

    #endregion
}