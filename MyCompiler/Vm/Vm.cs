using MyCompiler.Code;
using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.Buffers.Binary;

namespace MyCompiler.Vm;

public class Vm
{
    const int StackSize = 2048;

    readonly IObject[] constants;
    readonly byte[] instructions;

    readonly IObject[] stack;
    int sp;

    public Vm(Bytecode bytecode)
    {
        this.instructions = bytecode.Instructions;
        this.constants = bytecode.Constants;

        stack = new IObject[StackSize];
        sp = 0;

    }

    public Maybe Run()
    {
        var ip = 0;
        while (ip < instructions.Length)
        {
            var op = (Opcode)instructions[ip];

            switch (op)
            {
                case Opcode.OpConstant:
                    var constIndex = BinaryPrimitives.ReadUInt16BigEndian(instructions.AsSpan()[(ip + 1)..]);
                    ip += 2;
                    Push(constants[constIndex]);
                    break;

                case Opcode.OpAdd:
                    var right = (IntegerObject)Pop();
                    var left = (IntegerObject)Pop();

                    var result = left.Value + right.Value;
                    Push(new IntegerObject(result));
                    break;
                    //    case Opcode.OpSub:
                    //        right = Pop();
                    //        left = Pop();
                    //        result = left.Sub(right);
                    //        Push(result);
                    //        break;
                    //    case Opcode.OpMul:
                    //        right = Pop();
                    //        left = Pop();
                    //        result = left.Mul(right);
                    //        Push(result);
                    //        break;
                    //    case Opcode.OpDiv:
                    //        right = Pop();
                    //        left = Pop();
                    //        result = left.Div(right);
                    //        Push(result);
                    //        break;
                    //    case Opcode.OpPop:
                    //        Pop();
                    //        break;
                    //    default:
                    //        throw new Exception($"unknown opcode {op}");
            }

            ip++;
        }

        return Maybe.Ok;
    }

    private void Push(IObject constant)
    {
        stack[sp] = constant;
        sp++;
    }

    private IObject Pop()
    {
        --sp;
        return stack[sp];
    }

    public Maybe<IObject> StackTop()
    {
        if (sp == 0)
            return new Exception("Stack is empty");

        return Maybe<IObject>.From(stack[sp - 1]);
    }
}
