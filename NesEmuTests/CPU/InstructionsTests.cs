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
        var cpu = new NesEmu.CPU.CPU(program);

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
        var cpu = new NesEmu.CPU.CPU(program);

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
        var cpu = new NesEmu.CPU.CPU(program);

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
        var cpu = new NesEmu.CPU.CPU(program);
        
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
        var cpu = new NesEmu.CPU.CPU(program);

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
        var cpu = new NesEmu.CPU.CPU(program);

        // Act
        cpu.Interpret();

        //
        Assert.Equal(0, cpu.GetRegisterA());
        AssertStatusRegisterEqualNegativeFlag(cpu, false);
        AssertStatusRegisterEqualZeroFlag(cpu, true);
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