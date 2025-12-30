using NesEmu.Memory;

namespace NesEmuTests.CPU;

public class InstructionsTests
{
    #region LDA
    [Fact]
    public void TestLDA__WithPositiveValue__ShouldLoadValueAndSetFlags()
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
    public void TestLDA__WithNegativeValue__ShouldLoadValueAndSetFlags()
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
    public void Test_LDA__WithZeroValue__ShouldLoadValueAndSetFlags()
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
    [InlineData(0xFF,0x00, false, true)]
    [InlineData(0x00,0x01, false, false)]
    [InlineData(0xFD,0xFE, true, false)]
    [InlineData(0x02,0x03, false, false)]
    public void TextInx____ShouldIncrementAndSetFlags(byte previousValue, byte expectedFinalValue, bool expectedNegativeFlag,  bool expectedZeroFlag)
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