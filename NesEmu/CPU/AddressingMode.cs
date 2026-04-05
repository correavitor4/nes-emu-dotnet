namespace NesEmu.CPU;

public enum AddressingMode
{
    Immediate,
    ZeroPage,
    ZeroPageX,
    ZeroPageY,
    Absolute,
    AbsoluteX,
    AbsoluteY,
    IndirectX,
    IndirectY,
    Accumulator,
    NoneAddressing,
    Relative
}