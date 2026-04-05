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

    // For testing
    public void SetStatusFlag(byte newValue)
    {
        _status = newValue;
    }

    public void SetRegisterA(byte value)
    {
        _registerA = value;
    }

    #endregion

    #region RegisterStatusHandlers

    private void UpdateZeroFlag(byte value)
    {
        if (value == 0)
        {
            _status |= 0b0000_0010;
            return;
        }

        _status &= 0b1111_1101;
    }
    
    private void UpdateZeroFlag(int v)
    {
        var value = (byte)v;
        UpdateZeroFlag(value);
    }

    private void UpdateNegativeFlag(byte value)
    {
        if ((value & 0b1000_0000) != 0)
        {
            _status |= 0b1000_0000;
            return;
        }

        _status &= 0b0111_1111;
    }
    private void UpdateNegativeFlag(int value)
    {
        UpdateNegativeFlag((byte)value);
    }
    
    // TODO: test
    /// <summary>
    /// Updates the Overflow (V) flag based on signed arithmetic logic.
    /// Overflow occurs if the sign of the result is different from the sign of both 
    /// operands when both operands have the same sign.
    /// </summary>
    /// <param name="a">The original value in the Accumulator.</param>
    /// <param name="m">The operand value from memory.</param>
    /// <param name="result">The result of the operation (A + M + Carry).</param>
    public void UpdateOverflowFlag(byte a, byte m, byte result)
    {
        // Se (A ^ Result) AND (M ^ Result) tiver o Bit 7 definido, houve overflow.
        // Isso verifica se o sinal do resultado é diferente do sinal de AMBOS os operandos.
        bool overflow = ((a ^ result) & (m ^ result) & 0x80) != 0;

        if (overflow)
            _status |= 0b0100_0000; // Set V
        else
            _status &= 0b1011_1111; // Clear V
    }

    private void UpdateCarryFlag(int value)
    {

        var carry = value > 0xFF;
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

        // LDX
        _instructions.Add(0xA2, () => Ldx(AddressingMode.Immediate));
        _instructions.Add(0xA6, () => Ldx(AddressingMode.ZeroPage));
        _instructions.Add(0xB6, () => Ldx(AddressingMode.ZeroPageY));
        _instructions.Add(0xAE, () => Ldx(AddressingMode.Absolute));
        _instructions.Add(0xBE, () => Ldx(AddressingMode.AbsoluteY));

        
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
        
        // ASL
        _instructions.Add(0x0A, () => ASL(AddressingMode.Accumulator));
        _instructions.Add(0x06, () => ASL(AddressingMode.ZeroPage));
        _instructions.Add(0x16, () => ASL(AddressingMode.ZeroPageX));
        _instructions.Add(0x0E, () => ASL(AddressingMode.Absolute));
        _instructions.Add(0x1E, () => ASL(AddressingMode.AbsoluteX));
        
        // LDY
        _instructions.Add(0xA0, () => Ldy(AddressingMode.Immediate));
        _instructions.Add(0xA4, () => Ldy(AddressingMode.ZeroPage));
        _instructions.Add(0xB4, () => Ldy(AddressingMode.ZeroPageX));
        _instructions.Add(0xAC, () => Ldy(AddressingMode.Absolute));
        _instructions.Add(0xBC, () => Ldy(AddressingMode.AbsoluteX));
        
        // BCC
        _instructions.Add(0x90, () => Bcc(AddressingMode.Relative));
        
        // BCS
        _instructions.Add(0xB0, () => Bcs(AddressingMode.Relative));
        
        // BEQ
        _instructions.Add(0xF0, () => Beq(AddressingMode.Relative));
        
        // BIT
        _instructions.Add(0x24, (() => Bit(AddressingMode.ZeroPage)));
        _instructions.Add(0x2C, (() => Bit(AddressingMode.Absolute)));
        
        // BMI
        _instructions.Add(0x30, () => Bmi(AddressingMode.Relative));
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

        UpdateZeroFlag(_registerA);
        UpdateNegativeFlag(_registerA);
    }
    
    // TODO: test
    /// <summary>
    /// LDX instruction. It loads a value in memory and fill register_x value with it
    /// </summary>
    /// <param name="mode"></param>
    public void Ldx(AddressingMode mode)
    {
        var addr = GetOperandAddress(mode);
        var value = _nesMemory.Read(addr);
        _registerX = value;

        UpdateZeroFlag(_registerX);
        UpdateNegativeFlag(_registerX);
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

        UpdateZeroFlag(_registerX);
        UpdateNegativeFlag(_registerX);
    }


    /// <summary>
    /// INX instruction. It increments register_x value
    /// </summary>
    private void Inx()
    {
        _registerX++;
        UpdateNegativeFlag(_registerX);
        UpdateZeroFlag(_registerX);
    }

    /// <summary>
    /// ADD instruction (Add with carry). This instruction adds the contents of a memory location to the accumulator together with the carry bit. If overflow occurs the carry bit is set, this enables multiple byte addition to be performed.
    /// </summary>
    /// <param name="mode"></param>
    private void Adc(AddressingMode mode)
    {
        var operand = GetOperandAddress(mode);
        var value = _nesMemory.Read(operand);
        
        var temp = _registerA + value; // Addition itself

        UpdateCarryFlag(temp);
        UpdateZeroFlag(temp);
        UpdateOverflowFlag(_registerA, value, (byte)temp);
        UpdateNegativeFlag(temp);
        
        _registerA = (byte)temp;
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

        UpdateZeroFlag(_registerA);
        UpdateNegativeFlag(_registerA);
    }

    /// <summary>
    /// ASL Instruction 
    /// A,Z,C,N = M*2 or M,Z,C,N = M*2
    /// This operation shifts all the bits of the accumulator or memory contents one bit left. Bit 0 is set to 0 and bit 7 is placed in the carry flag. The effect of this operation is to multiply the memory contents by 2 (ignoring 2's complement considerations), setting the carry if the result will not fit in 8 bits.
    /// </summary>
    /// <param name="mode"></param>
    private void ASL(AddressingMode mode)
    {
        byte value;
        ushort? operand = null;
        if (mode.Equals(AddressingMode.Accumulator))
        {
            value = _registerA;
        }
        else
        {
            operand = GetOperandAddress(mode);
            value = _nesMemory.Read(operand ?? throw new ArgumentNullException());
        }
        
        var temp = value << 1;
        
        UpdateCarryFlag(temp);
        UpdateZeroFlag(temp);
        UpdateNegativeFlag(temp);

        if (mode.Equals(AddressingMode.Accumulator))
        {
            _registerA = (byte)temp;
            return;
        }
        
        _nesMemory.Write(operand ?? throw new ArgumentNullException(), (byte)temp);
    }

    /// <summary>
    /// LDY instruction. Loads a byte of memory into the Y register setting the zero and negative flags as appropriate.
    /// </summary>
    /// <param name="mode"></param>
    private void Ldy(AddressingMode mode)
    {
        var param = GetOperandAddress(mode);
        var value = _nesMemory.Read(param);
        
        _registerY = value;
        UpdateZeroFlag(_registerY);
        UpdateNegativeFlag(_registerY);
    }

    /// <summary>
    /// BCC instruction. If the carry flag is clear then add the relative displacement to the program counter to cause a branch to a new location.
    /// </summary>
    /// <param name="mode">Refers to Addressing mode</param>
    private void Bcc(AddressingMode mode)
    {
        if ((_status & 0b0000_0001) != 0b0000_0000)
        {
            ProgramCounter++;
            return;
        }
        
        if (!mode.Equals(AddressingMode.Relative)) throw new InvalidEnumArgumentException("Only relative addressing mode is supported during BCC instruction");
        
        var param = GetOperandAddress(mode);
        var value = (sbyte)_nesMemory.Read(param);

        ProgramCounter = (ushort)(ProgramCounter + value);
    }
    
    /// <summary>
    /// BCS instruction. If the carry flag is set then add the relative displacement to the program counter to cause a branch to a new location.
    /// </summary>
    /// <param name="mode">Refers to Addressing mode</param>
    private void Bcs(AddressingMode mode)
    {
        if ((_status & 0b0000_0001) != 0b0000_0001)
        {
            ProgramCounter++;
            return;
        }
        
        if (!mode.Equals(AddressingMode.Relative)) throw new InvalidEnumArgumentException("Only relative addressing mode is supported during BCC instruction");
        
        var param = GetOperandAddress(mode);
        var value = (sbyte)_nesMemory.Read(param);
        ProgramCounter = (ushort)(ProgramCounter + value);
    }

    
    /// <summary>
    /// BEQ instruction. If the zero flag is set then add the relative displacement to the program counter to cause a branch to a new location.
    /// </summary>
    /// <param name="mode">Refers to Addressing mode</param>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    private void Beq(AddressingMode mode)
    {
        if ((_status & 0b0000_0010) != 0b0000_0010)
        {
            ProgramCounter++;
            return;
        }
        
        if (!mode.Equals(AddressingMode.Relative)) throw new InvalidEnumArgumentException("Only relative addressing mode is supported");
        
        var param = GetOperandAddress(mode);
        var value = (sbyte)_nesMemory.Read(param);
        ProgramCounter = (ushort)(ProgramCounter + value);
    }

    /// <summary>
    /// BIT instruction. A & M, N = M7, V = M6
    /// This instructions is used to test if one or more bits are set in a target memory location. The mask pattern in A is ANDed with the value in memory to set or clear the zero flag, but the result is not kept. Bits 7 and 6 of the value from memory are copied into the N and V flags.
    /// </summary>
    /// <param name="mode"></param>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    private void Bit(AddressingMode mode)
    {
        var addr = GetOperandAddress(mode);
        var value = _nesMemory.Read(addr);

        // 1. Zero Flag: Z = (A AND M) == 0
        // Note: usamos != 0 para saber se o resultado contém bits, 
        // e invertemos para a flag Zero (Z=1 se o resultado for 0).
        if ((_registerA & value) == 0)
        {
            _status |= 0b0000_0010; // Liga bit 1 (Zero)
        }
        else
        {
            _status &= 0b1111_1101; // Desliga bit 1 (Zero)
        }

        // 2. Negative e Overflow: Copia bits 7 e 6 DIRETAMENTE do valor lido
        // Primeiro, limpamos os bits 7 e 6 atuais do status para não "sujar"
        _status &= 0b0011_1111; 
    
        // Agora pegamos apenas os bits 7 e 6 do 'value' e injetamos no status
        _status |= (byte)(value & 0b1100_0000);
    }

    /// <summary>
    ///  BMI instruction. If the negative flag is set then add the relative displacement to the program counter to cause a branch to a new location.
    /// </summary>
    /// <param name="mode">Addressing mode (should be always "relative")</param>
    private void Bmi(AddressingMode mode)
    {
        if (!mode.Equals(AddressingMode.Relative))  throw new InvalidEnumArgumentException("Only relative addressing mode is supported");
        if ((_status & 0b1000_0000) != 0b1000_0000)
        {
            ProgramCounter++;
            return;
        }
        
        var param = GetOperandAddress(mode);
        var value = (sbyte)_nesMemory.Read(param);
        ProgramCounter = (ushort)(ProgramCounter + value);
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
            
            case AddressingMode.Relative:
                return ProgramCounter++;
            
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