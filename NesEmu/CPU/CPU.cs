using System.ComponentModel;

namespace NesEmu.CPU;

public class CPU
{
    private byte _registerA = 0;
    private byte _registerX = 0;
    private byte _registerY = 0;
    private byte _status = 0;
    public ushort ProgramCounter = 0;

    private readonly Memory.NesMemory _nesMemory;
    private Dictionary<byte, Action> _instructions = new Dictionary<byte, Action>();

    public CPU(Memory.NesMemory nesMemory)
    {
        this._nesMemory = nesMemory;
        RegisterInstructions();
    }

    public void Interpret()
    {
        while (ProgramCounter < _nesMemory.Length)
        {
            var opcode = _nesMemory.Read(ProgramCounter);
            ProgramCounter++;

            _instructions[opcode]();
        }
    }

    //For testing
    public void Interpret(int limit)
    {
        while (ProgramCounter < _nesMemory.Length && limit > 0)
        {
            var opcode = _nesMemory.Read(ProgramCounter);
            ProgramCounter++;

            _instructions[opcode]();
            limit -= 1;
        }
    }

    private void Reset()
    {
        ResetAllRegisters();
        ResetRegisterStatus();

        ProgramCounter = _nesMemory.ReadLittleEndian(0xFFCC);
    }

    private void Load()
    {
        _nesMemory.WriteLittleEndian(0xFFCC, 0x8000);
    }

    public void LoadAndInterpret()
    {
        Load();
        Reset();
        Interpret();
    }

    private void ResetAllRegisters()
    {
        _registerA = 0;
        _registerX = 0;
    }

    #region Getters

    public byte GetRegisterX()
    {
        return _registerX;
    }

    public byte GetRegisterStatus()
    {
        return _status;
    }

    public byte GetRegisterA()
    {
        return _registerA;
    }

    public int GetRegisterY()
    {
        return _registerY;
    }

    #endregion

    #region Setters

    public void SetRegisterX(byte value)
    {
        _registerX = value;
    }

    public void SetRegisterY(byte value)
    {
        _registerY = value;
    }

    #endregion

    #region RegisterStatusHandlers

    private void RegisterStatusSetZeroFlag(byte value)
    {
        if (value == 0)
        {
            _status |= 0b0000_0010;
            return;
        }

        _status &= 0b1111_1101;
    }

    private void RegisterStatusSetNegativeFlag(byte value)
    {
        if ((value & 0b1000_0000) != 0)
        {
            _status |= 0b1000_0000;
            return;
        }

        _status &= 0b0111_1111;
    }

    public void RegisterStatusSetOverFlowFlag(byte a, byte b)
    {
        var c6 = ((a & 0b0100_0000) == 0b0100_0000) &&
                 ((b & 0b0100_0000) == 0b0100_0000); // C6 is true when both m6 and n6 equals 1
        var m7 = (b & 0b1000_0000) == 0b1000_0000;
        var n7 = ((a & 0b1000_0000) == 0b1000_0000);

        var overflow = (!m7 & !n7 & c6) | (m7 & n7 & !c6);

        if (overflow)
        {
            _status = (byte)(_status | 0b0100_0000);
            return;
        }

        _status = (byte)(_status & 0b1011_1111);
    }

    public void RegisterStatusSetCarryBitFlag(byte a, byte b)
    {
        var c6 = ((a & 0b0100_0000) == 0b0100_0000) &&
                 ((b & 0b0100_0000) == 0b0100_0000); // C6 is true when both m6 and n6 equals 1
        var m7 = (b & 0b1000_0000) == 0b1000_0000;
        var n7 = ((a & 0b1000_0000) == 0b1000_0000);

        var carry = (c6 && m7) || (c6 && n7) || (m7 && n7); // carry bit equals one if at least 2 of 3 are true
        if (carry)
        {
            _status = (byte)(_status | 0b0000_0001);
            return;
        }

        _status = (byte)(_status & 0b1111_1110);
    }

    private void RegisterInstructions()
    {
        // LDA
        _instructions.Add(0xA9, () => Lda(AddressingMode.Immediate));
        _instructions.Add(0xA5, () => Lda(AddressingMode.ZeroPage));
        _instructions.Add(0xB5, () => Lda(AddressingMode.ZeroPageX));
        _instructions.Add(0xAD, () => Lda(AddressingMode.Absolute));
        _instructions.Add(0xBD, () => Lda(AddressingMode.AbsoluteX));
        _instructions.Add(0xB9, () => Lda(AddressingMode.AbsoluteY));
        _instructions.Add(0xA1, () => Lda(AddressingMode.IndirectX));
        _instructions.Add(0xB1, () => Lda(AddressingMode.IndirectY));

        // BRK
        _instructions.Add(0x00, Brk);

        //TAX
        _instructions.Add(0xAA, Tax);

        // INX
        _instructions.Add(0xE8, Inx);

        // ADC
        _instructions.Add(0x69, () => Adc(AddressingMode.Immediate));
        _instructions.Add(0x65, () => Adc(AddressingMode.ZeroPage));
        _instructions.Add(0x75, () => Adc(AddressingMode.ZeroPageX));
        _instructions.Add(0x6D, () => Adc(AddressingMode.Absolute));
        _instructions.Add(0x7D, () => Adc(AddressingMode.AbsoluteX));
        _instructions.Add(0x79, () => Adc(AddressingMode.AbsoluteY));
        _instructions.Add(0x61, () => Adc(AddressingMode.IndirectX));
        _instructions.Add(0x71, () => Adc(AddressingMode.IndirectY));

        // AND
        _instructions.Add(0x29, () => And(AddressingMode.Immediate));
        _instructions.Add(0x25, () => And(AddressingMode.ZeroPage));
        _instructions.Add(0x35, () => And(AddressingMode.ZeroPageX));
        _instructions.Add(0x2D, () => And(AddressingMode.Absolute));
        _instructions.Add(0x3D, () => And(AddressingMode.AbsoluteX));
        _instructions.Add(0x39, () => And(AddressingMode.AbsoluteY));
        _instructions.Add(0x21, () => And(AddressingMode.IndirectX));
        _instructions.Add(0x31, () => And(AddressingMode.IndirectY));
    }

    private void ResetRegisterStatus()
    {
        _status = 0x00;
    }

    #endregion

    #region Instructions

    /// <summary>
    /// LDA instruction. It loads a value in memory and fill register_a value with it
    /// </summary>
    /// <param name="mode"></param>
    public void Lda(AddressingMode mode)
    {
        var addr = GetOperandAddress(mode);
        var value = _nesMemory.Read(addr);
        _registerA = value;

        RegisterStatusSetZeroFlag(_registerA);
        RegisterStatusSetNegativeFlag(_registerA);
    }

    /// <summary>
    /// Do nothing
    /// </summary>
    private void Brk()
    {
        return;
    }

    /// <summary>
    /// TAX instruction. It copy value from register_a to register_x
    /// </summary>
    private void Tax()
    {
        _registerX = _registerA;

        RegisterStatusSetZeroFlag(_registerX);
        RegisterStatusSetNegativeFlag(_registerX);
    }


    /// <summary>
    /// INX instruction. It increments register_x value
    /// </summary>
    private void Inx()
    {
        _registerX++;
        RegisterStatusSetNegativeFlag(_registerX);
        RegisterStatusSetZeroFlag(_registerX);
    }

    /// <summary>
    /// ADD instruction (Add with carry). This instruction adds the contents of a memory location to the accumulator together with the carry bit. If overflow occurs the carry bit is set, this enables multiple byte addition to be performed.
    /// </summary>
    /// <param name="mode"></param>
    private void Adc(AddressingMode mode)
    {
        var operand = GetOperandAddress(mode);
        var value = _nesMemory.Read(operand);

        var tempA = _registerA;

        _registerA = (byte)(_registerA + value); // Addition itself

        RegisterStatusSetCarryBitFlag(tempA, value);
        RegisterStatusSetZeroFlag(_registerA);
        RegisterStatusSetOverFlowFlag(tempA, value);
        RegisterStatusSetNegativeFlag(_registerA);
    }

    /// <summary>
    /// AND instruction.
    /// A,Z,N = A&M
    /// A logical AND is performed, bit by bit, on the accumulator contents using the contents of a byte of memory.
    /// </summary>
    /// <param name="mode"></param>
    private void And(AddressingMode mode)
    {
        var operand = GetOperandAddress(mode);
        var value = _nesMemory.Read(operand);

        _registerA = (byte)(_registerA & value);

        RegisterStatusSetZeroFlag(_registerA);
        RegisterStatusSetNegativeFlag(_registerA);
    }

    #endregion

    #region Addressing

    public ushort GetOperandAddress(AddressingMode mode)
    {
        byte addrByte;
        ushort addrUshort;

        switch (mode)
        {
            case AddressingMode.Immediate:
                return ProgramCounter++;

            case AddressingMode.ZeroPage:
                addrByte = GetAddressZeroPage();
                ProgramCounter++;
                return addrByte;

            case AddressingMode.ZeroPageX:
                addrByte = GetZeroPageX();
                ProgramCounter++;
                return addrByte;

            case AddressingMode.ZeroPageY:
                addrByte = GetZeroPageY();
                ProgramCounter++;
                return addrByte;

            case AddressingMode.Absolute:
                addrUshort = GetAddressAbsolute();
                ProgramCounter += 2;
                return addrUshort;

            case AddressingMode.AbsoluteX:
                addrUshort = GetAbsoluteX();
                ProgramCounter += 2;
                return addrUshort;

            case AddressingMode.AbsoluteY:
                addrUshort = GetAbsoluteY();
                ProgramCounter += 2;
                return addrUshort;

            case AddressingMode.IndirectX:
                addrUshort = GetIndirectX();
                ProgramCounter++;
                return addrUshort;
                
            case AddressingMode.IndirectY:
                addrUshort = GetIndirectY();
                ProgramCounter++;
                return addrUshort;
            
            default:
                throw new InvalidEnumArgumentException("mode", (int)mode, typeof(AddressingMode));
        }
    }

    private ushort GetIndirectX()
    {
        var baseAddr = _nesMemory.Read(ProgramCounter);
        var ptr = WappingAdd(baseAddr, _registerX);

        var lo = _nesMemory.Read(ptr);
        var hi = _nesMemory.Read(WappingAdd(ptr, 1));
        return (ushort)(hi << 8 | lo);
    }

    private ushort GetIndirectY()
    {
        var baseAddr = _nesMemory.Read(ProgramCounter); // Base ZP

        var lo = _nesMemory.Read(baseAddr);
        var hi = _nesMemory.Read(WappingAdd(baseAddr, (byte)1));
        var ptr = (ushort)((hi << 8) | lo); // Ponteiro 16-bit

        return WappingAdd(ptr, _registerY); // ptr + Y
    }

    private ushort GetAbsoluteX()
    {
        var baseAddr = _nesMemory.ReadLittleEndian(ProgramCounter);
        return WappingAdd(baseAddr, _registerX);
    }

    private ushort GetAbsoluteY()
    {
        var baseAddr = _nesMemory.ReadLittleEndian(ProgramCounter);
        return WappingAdd(baseAddr, _registerY);
    }

    private byte GetZeroPageY()
    {
        var baseAddr = _nesMemory.Read(ProgramCounter);
        return WappingAdd(baseAddr, _registerY);
    }

    private byte GetZeroPageX()
    {
        var baseAddr = _nesMemory.Read(ProgramCounter);
        return WappingAdd(baseAddr, _registerX);
    }

    private byte WappingAdd(byte a, byte b)
    {
        return (byte)(a + b);
    }

    private ushort WappingAdd(ushort a, byte b)
    {
        return (ushort)(a + b);
    }

    private byte GetAddressZeroPage()
    {
        return _nesMemory.Read(ProgramCounter);
    }

    private ushort GetAddressAbsolute()
    {
        return _nesMemory.ReadLittleEndian(ProgramCounter);
    }

    #endregion
}