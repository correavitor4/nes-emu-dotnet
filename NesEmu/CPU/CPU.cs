using System.ComponentModel;

namespace NesEmu.CPU;

public class CPU
{
    private byte _registerA = 0;
    private byte _registerX  = 0;
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
        ProgramCounter = 0;

        while (ProgramCounter < _nesMemory.Length)
        {
            var opcode = _nesMemory.Read(ProgramCounter);
            ProgramCounter++;

            _instructions[opcode]();
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

    private void RegisterInstructions()
    {
        _instructions.Add(0xA9, () => Lda());
        _instructions.Add(0x00, Brk);
        _instructions.Add(0xAA, Tax);
        _instructions.Add(0xE8, Inx);
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
    private void Lda()
    {
        var param = _nesMemory.Read(ProgramCounter);
        ProgramCounter++;
        _registerA = param;


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

    private void Inx()
    {
        _registerX++;
        RegisterStatusSetNegativeFlag(_registerX);
        RegisterStatusSetZeroFlag(_registerX);
    }

    #endregion

    #region Addressing

    public ushort GetOperandAddress(AddressingMode mode)
    {
        switch (mode)
        {
            case AddressingMode.Immediate:
                return ProgramCounter;
            case AddressingMode.ZeroPage:
                return GetAddressZeroPage();
            case AddressingMode.Absolute:
                return GetAddressAbsolute();
            case AddressingMode.ZeroPageX:
                return GetZeroPageX();
            case AddressingMode.ZeroPageY:
                return GetZeroPageY();
            case AddressingMode.AbsoluteX:
                return GetAbsoluteX();
            case AddressingMode.AbsoluteY:
                return GetAbsoluteY();
            case AddressingMode.IndirectX:
                return GetIndirectX();

            case AddressingMode.IndirectY:
                return GetIndirectY();
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