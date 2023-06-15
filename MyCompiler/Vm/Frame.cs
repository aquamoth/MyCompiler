using MyCompiler.Code;
using MyCompiler.Entities;
using System.Buffers.Binary;

namespace MyCompiler.Vm;

record Frame(CompiledFunction CompiledFunction)
{
    int ip = -1;

    public byte[] Instructions => CompiledFunction.Instructions;

    public bool TryReadInstruction(out Opcode opcode)
    {
        if (ip >= Instructions.Length - 1)
        {
            opcode = Opcode.OpNull;
            return false;
        }

        ip++;
        opcode = (Opcode)Instructions[ip];
        return true;
    }

    public ushort ReadConstant()
    {
        var constIndex = BinaryPrimitives.ReadUInt16BigEndian(Instructions.AsSpan()[(ip + 1)..]);
        ip += 2;
        return constIndex;
    }

    public void Jump(int newIp)
    {
        ip = (ushort)newIp;
    }
}
