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

    public ushort ReadLittleEndian(ushort address)
    {
        var lo = Read(address);     // Byte baixo (address)
        var hi = Read(++address);   // Byte alto (address+1)
        return (ushort)((hi << 8) | lo);  // ✅ Little endian correto
    }

    public void WriteLittleEndian(ushort address, ushort value)  // ← ushort!
    {
        Write(address, (byte)value);        // Byte baixo em address
        Write(++address, (byte)(value >> 8)); // Byte alto em address+1
    }


    public void Write(ushort address, byte value)
    {
        MemorySpace[address] = value;
    }

    public byte[] ToBytesArray()
    {
        return MemorySpace;
    }

    #region Static 
    
    public static NesMemory FromBytesArray(byte[] array)
    {
        var mem = new NesMemory(array.Length);
        for (int i = 0; i < mem.Length; i++)
        {
            mem.Write((ushort)i, array[i]);
        }
        
        return mem;
    }
    
    #endregion
    
}
