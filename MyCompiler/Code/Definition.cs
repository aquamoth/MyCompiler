namespace MyCompiler.Code;

public readonly struct Definition
{
    public readonly Opcode Opcode { get; init; }
    public readonly string Name { get; init; }
    public readonly int[] OperandWidths { get; init; }
}
