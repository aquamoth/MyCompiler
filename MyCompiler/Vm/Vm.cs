using MyCompiler.Code;
using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.Buffers.Binary;

namespace MyCompiler.Vm;

public class Vm
{
    const int STACK_SIZE = 2048;
    public const int GLOBALS_SIZE = 65536;

    readonly byte[] instructions;
    readonly IObject[] constants;
    readonly IObject[] globals;
    readonly IObject[] stack;
    int sp;

    public Vm(Bytecode bytecode, IObject[] globals)
    {
        this.instructions = bytecode.Instructions;
        this.constants = bytecode.Constants;
        this.globals = globals;

        stack = new IObject[STACK_SIZE];
        sp = 0;
    }

    public Vm(Bytecode bytecode) : this(bytecode, new IObject[GLOBALS_SIZE])
    {
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
                    {
                        var constIndex = BinaryPrimitives.ReadUInt16BigEndian(instructions.AsSpan()[(ip + 1)..]);
                        ip += 2;
                        Push(constants[constIndex]);
                    }
                    break;

                case Opcode.OpNull:
                    Push(NullObject.Value);
                    break;

                case Opcode.OpTrue:
                    Push(BooleanObject.True);
                    break;

                case Opcode.OpFalse:
                    Push(BooleanObject.False);
                    break;

                case Opcode.OpAdd:
                    {
                        var right = Pop();
                        var left = Pop();

                        if (left is IntegerObject leftInt && right is IntegerObject rightInt)
                        {
                            Push(new IntegerObject(leftInt.Value + rightInt.Value));
                        }
                        else if (left is StringObject leftString && right is StringObject rightString)
                        {
                            Push(new StringObject(leftString.Value + rightString.Value));
                        }
                        else
                        {
                            return new Exception($"unable to add types {left.Type} + {right.Type}");
                        }

                    }
                    break;

                case Opcode.OpSub:
                    {
                        var right = (IntegerObject)Pop();
                        var left = (IntegerObject)Pop();
                        var result = left.Value - right.Value;
                        Push(new IntegerObject(result));
                    }
                    break;

                case Opcode.OpMul:
                    {
                        var right = (IntegerObject)Pop();
                        var left = (IntegerObject)Pop();
                        var result = left.Value * right.Value;
                        Push(new IntegerObject(result));
                    }
                    break;

                case Opcode.OpDiv:
                    {
                        var right = (IntegerObject)Pop();
                        var left = (IntegerObject)Pop();
                        var result = left.Value / right.Value;
                        Push(new IntegerObject(result));
                    }
                    break;

                case Opcode.OpEqual:
                case Opcode.OpNotEqual:
                case Opcode.OpGreaterThan:
                    {
                        var result = ExecuteComparison(op);
                        if (result.HasError)
                            return result;
                    }
                    break;

                case Opcode.OpMinus:
                    {
                        var operand = Pop();
                        if (operand is not IntegerObject integer)
                            return new Exception($"unsupported type for negation: {op}{operand.Type}");
                        Push(new IntegerObject(-integer.Value));
                    }
                    break;

                case Opcode.OpBang:
                    ExecuteBangOperator();
                    break;

                case Opcode.OpPop:
                    Pop();
                    break;

                case Opcode.OpJumpNotTruthy:
                    {
                        var pos = BinaryPrimitives.ReadUInt16BigEndian(instructions.AsSpan()[(ip + 1)..]);
                        ip += 2;

                        var condition = Pop();
                        if (!IsTruthy(condition))
                            ip = pos - 1;
                    }
                    break;

                case Opcode.OpJump:
                    {
                        var pos = BinaryPrimitives.ReadUInt16BigEndian(instructions.AsSpan()[(ip + 1)..]);
                        ip = pos - 1;
                    }
                    break;

                case Opcode.OpSetGlobal:
                    {
                        var globalIndex = BinaryPrimitives.ReadUInt16BigEndian(instructions.AsSpan()[(ip + 1)..]);
                        ip += 2;

                        globals[globalIndex] = Pop();
                    }
                    break;

                case Opcode.OpGetGlobal:
                    {
                        var globalIndex = BinaryPrimitives.ReadUInt16BigEndian(instructions.AsSpan()[(ip + 1)..]);
                        ip += 2;

                        Push(globals[globalIndex]);
                    }
                    break;

                case Opcode.OpArray:
                    {
                        var size = BinaryPrimitives.ReadUInt16BigEndian(instructions.AsSpan()[(ip + 1)..]);
                        ip += 2;

                        var array = new IObject[size];
                        for (var i = size - 1; i >= 0; i--)
                            array[i] = Pop();
                        Push(new ArrayObject(array));
                    }
                    break;

                default:
                    return new Exception($"unknown opcode {op}");
            }

            ip++;
        }

        return Maybe.Ok;
    }

    private void ExecuteBangOperator()
    {
        var operand = Pop();
        var result = operand switch
        {
            NullObject => BooleanObject.True,
            BooleanObject boolean => boolean.Value ? BooleanObject.False : BooleanObject.True,
            _ => BooleanObject.False
        };

        Push(result);
    }

    private static bool IsTruthy(IObject operand)
    {
        return operand switch
        {
            BooleanObject boolean => boolean.Value,
            NullObject => false,
            _ => true
        };
    }

    private Maybe ExecuteComparison(Opcode op)
    {
        var right = Pop();
        var left = Pop();

        if (left is IntegerObject leftInt && right is IntegerObject rightInt)
            return ExecuteIntegerComparison(op, leftInt.Value, rightInt.Value);

        if (left is BooleanObject leftBool && right is BooleanObject rightBool)
            return ExecuteBooleanComparison(op, leftBool.Value, rightBool.Value);

        return new Exception($"type mismatch: {left.Type} {op} {right.Type}");
    }

    private Maybe ExecuteIntegerComparison(Opcode op, long left, long right)
    {
        switch (op)
        {
            case Opcode.OpEqual:
                Push(left == right ? BooleanObject.True : BooleanObject.False);
                return Maybe.Ok;

            case Opcode.OpNotEqual:
                Push(left != right ? BooleanObject.True : BooleanObject.False);
                return Maybe.Ok;

            case Opcode.OpGreaterThan:
                Push(left > right ? BooleanObject.True : BooleanObject.False);
                return Maybe.Ok;

            default:
                return new Exception($"unsupported integer comparison: {op}");
        }
    }

    private Maybe ExecuteBooleanComparison(Opcode op, bool left, bool right)
    {
        switch (op)
        {
            case Opcode.OpEqual:
                Push(left == right ? BooleanObject.True : BooleanObject.False);
                return Maybe.Ok;

            case Opcode.OpNotEqual:
                Push(left != right ? BooleanObject.True : BooleanObject.False);
                return Maybe.Ok;

            default:
                return new Exception($"unsupported boolean comparison: {op}");
        }
    }

    private void Push(IObject constant)
    {
        if (sp == STACK_SIZE)
            throw new Exception("stack overflow");

        stack[sp] = constant;
        sp++;
    }

    private IObject Pop()
    {
        if (sp == 0)
            throw new Exception("stack underflow");

        --sp;
        return stack[sp];
    }

    public IObject LastPoppedStackElem()
    {
        return stack[sp];
    }
}
