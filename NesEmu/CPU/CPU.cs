namespace NesEmu.CPU;

public class CPU
{
    private byte _registerA = 0;
    private byte _registerX = 0;
    private byte _status = 0;
    private ushort _programCounter = 0;
    private readonly byte[] _program;
    private readonly Dictionary<byte, Action> _instructions = new Dictionary<byte, Action>();

    public CPU(byte[] program)
    {
        this._program = program;
        RegisterInstructions();
    }

    public void Interpret()
    {
        _programCounter = 0;

        while (_programCounter < _program.Length)
        {
            var opcode = _program[_programCounter];
            _programCounter++;

            _instructions[opcode]();
        }
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

    #endregion

    #region Instructions

    private void RegisterInstructions()
    {
        _instructions.Add(0xA9, Lda);
        _instructions.Add(0x00, Brk);
        _instructions.Add(0xAA, Tax);
        _instructions.Add(0xE8, Inx);
    }

    /// <summary>
    /// LDA instruction. It loads a value in memory and fill register_a value with it
    /// </summary>
    private void Lda()
    {
        var param = _program[_programCounter];
        _programCounter++;
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
}