namespace MyCompiler.Code;

readonly record struct EmittedInstruction(Opcode Opcode, int Position);
