using MyCompiler.Entities;

namespace MyCompiler.Code;

public struct Bytecode
{
    public byte[] Instructions { get; init; }
    public IObject[] Constants { get; init; }
}