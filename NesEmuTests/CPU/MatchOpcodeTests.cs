namespace NesEmuTests.CPU;

public class MatchOpcodeTests
{
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
       Assert.Equal(0b0000_0000, cpu.GetRegisterStatus() & 0b0000_0010); // status register not equals 0
       Assert.Equal(0b0000_0000, cpu.GetRegisterStatus() & 0b1000_0000); // status register non negative
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
        Assert.Equal(0b0000_0000, cpu.GetRegisterStatus() & 0b0000_0010); // status register not equals zero
        Assert.Equal(0b1000_0000, cpu.GetRegisterStatus() & 0b1000_0000); // status register equals negative 
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
        Assert.Equal(0b0000_0010, cpu.GetRegisterStatus() & 0b0000_0010); // Status register equals zero
        Assert.Equal(0b0000_0000, cpu.GetRegisterStatus() & 0b1000_0000); // Status register non negative
    }
}