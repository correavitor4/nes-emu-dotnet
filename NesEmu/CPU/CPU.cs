namespace NesEmu.CPU;

public class CPU
{
    private byte _registerA = 0;
    private byte _status  = 0;
    private ushort _programCounter = 0;
    private readonly byte[] _program;
    
    public CPU(byte[] program)
    {
        this._program = program;
    }

    public void Interpret()
    {
        _programCounter = 0;

        while (_programCounter < _program.Length)
        {
            var opcode = _program[_programCounter];
            _programCounter++;
            
            MatchOpcode(opcode);
        }
    }

    private void MatchOpcode(byte opcode)
    {
        switch (opcode)
        {
            case 0xA9:
                var param = _program[_programCounter];
                _programCounter++;
                _registerA = param;

                if (_registerA == 0)
                {
                    _status |= 0b0000_0010;
                }
                else
                {
                    _status &= 0b1111_1101;
                }

                if ((_registerA & 0b1000_0000) != 0)
                {
                    _status |= 0b1000_0000;
                }
                else
                {
                    _status &= 0b0111_1111;
                }
                break;
            case 0x00:
                return;
        }
    }

    #region Getters

    public byte GetRegisterStatus()
    {
        return _status;
    }

    public byte GetRegisterA()
    {
        return _registerA;
    }
    
    #endregion
}