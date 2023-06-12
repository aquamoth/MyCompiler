﻿using MyCompiler.Code;
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
                        var right = (IntegerObject)Pop();
                        var left = (IntegerObject)Pop();
                        var result = left.Value + right.Value;
                        Push(new IntegerObject(result));
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
                            return result.Error!;
                    }
                    break;

                case Opcode.OpMinus:
                    {
                        var right = Pop();
                        if (right is not IntegerObject rightInt)
                            return new Exception($"unsupported type for negation: {op}{right.Type}");
                        Push(new IntegerObject(-rightInt.Value));
                    }
                    break;

                case Opcode.OpBang:
                    {
                        var right = Pop();
                        Push(right == BooleanObject.False ? BooleanObject.True : BooleanObject.False);
                    }
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

                default:
                    return new Exception($"unknown opcode {op}");
            }

            ip++;
        }

        return Maybe.Ok;
    }

    private static bool IsTruthy(IObject condition)
    {
        return condition switch
        {
            BooleanObject boolean => boolean.Value,
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
        if (sp == StackSize)
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
