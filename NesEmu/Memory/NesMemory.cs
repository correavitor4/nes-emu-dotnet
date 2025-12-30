namespace NesEmu.Memory;

public class NesMemory(int memorySizeInBytes = 0xFFFF)
{
    public byte[] MemorySpace = new byte[memorySizeInBytes];
    public int Length
    {
        get
        {
            return MemorySpace.Length;
        }
    }
    public byte Read(ushort address)
    {
        return MemorySpace[address];
    }

    public void Write(ushort address, byte value)
    {
        MemorySpace[address] = value;
    }

    public static NesMemory FromBytesArray(byte[] array)
    {
        var mem = new NesMemory(array.Length);
        for (int i = 0; i < mem.Length; i++)
        {
            mem.Write((ushort)i, array[i]);
        }
        
        return mem;
    }
}