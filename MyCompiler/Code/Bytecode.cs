namespace MyCompiler.Code;

public struct Bytecode
{
    public byte[] Instructions { get; init; }
    public object[] Constants { get; init; }
}