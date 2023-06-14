namespace MyCompiler.Code;

record CompilationScope(List<byte> Instructions)
{
    public EmittedInstruction LastInstruction { get; private set; }
    internal EmittedInstruction PrevInstruction;

    public CompilationScope() : this(new List<byte>()) { }

    public int AddInstruction(byte[] ins)
    {
        var posNewInstruction = Instructions.Count;
        Instructions.AddRange(ins);
        return posNewInstruction;
    }

    public void ReplaceInstruction(int position, Span<byte> newInstruction)
    {
        for (var i = 0; i < newInstruction.Length; i++)
            Instructions[position + i] = newInstruction[i];
    }

    public void RemoveLastPop()
    {
        Instructions.RemoveAt(Instructions.Count - 1);
        LastInstruction = PrevInstruction;
    }

    public void SetLastInstruction(Opcode opcode, int pos)
    {
        PrevInstruction = LastInstruction;
        LastInstruction = new EmittedInstruction(opcode, pos);
    }
}
