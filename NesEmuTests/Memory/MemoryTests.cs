using NesEmu.Memory;

namespace NesEmuTests.Memory;

public class MemoryTests
{
    [Fact]
    public void TestReadLittleEndian____ShouldReturnInvertedHiAndLo()
    {
        // Arrange
        var program = new byte[8];
        program[0] = 0xFF;
        program[1] = 0xDD;
        
        var mem = NesMemory.FromBytesArray(program);
        
        // Act
        var res = mem.ReadLittleEndian(0x0000);
        

        //Assert
        Assert.Equal(0xDDFF, res);
    }

    [Fact]
    public void TestWriteLittleEndian____ShouldReturnInvertedHiAndLo()
    {
        // Arrange
        var mem = new NesMemory(2);
        var valueToWrite = (ushort)0xFFDD;
        
    
        // Act
        mem.WriteLittleEndian(0x0000, valueToWrite);
        

        // Assert
        Assert.Equal(0xDD, mem.Read(0x00));
        Assert.Equal(0xFF, mem.Read(0x01));
    }
}