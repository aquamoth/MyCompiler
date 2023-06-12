using MyCompiler.Entities;

namespace MyCompiler.Code;

public record struct Bytecode(byte[] Instructions, IObject[] Constants)
{
}