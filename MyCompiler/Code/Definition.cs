namespace MyCompiler.Code;

public readonly record struct Definition(Opcode Opcode, string Name, int[] OperandWidths);
