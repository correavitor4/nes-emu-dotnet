namespace NesEmu.Exceptions;

public class IllegalOpcodeException : Exception
{
    public IllegalOpcodeException(string message) : base(message) { }

    public IllegalOpcodeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}