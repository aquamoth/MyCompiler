namespace MyCompiler.Code;

public enum Opcode : byte
{
    OpConstant = 0x00,
    OpAdd,
    OpSub,
    OpMul,
    OpDiv,
    OpPop,
}
