﻿using MyCompiler.Code;
using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler.Vm;

public class Vm
{
    const int MAX_FRAMES = 1024;
    const int STACK_SIZE = 2048;
    public const int GLOBALS_SIZE = 65536;


    readonly IObject[] constants;
    readonly IObject[] globals;
    readonly IObject[] stack;
    int sp;

    readonly Stack<Frame> frames;

    public Vm(Bytecode bytecode, IObject[] globals)
    {
        var mainFn = new CompiledFunction(bytecode.Instructions, 0);

        frames = new Stack<Frame>(MAX_FRAMES);
        frames.Push(new Frame(mainFn, 0));

        this.constants = bytecode.Constants;
        this.globals = globals;

        stack = new IObject[STACK_SIZE];
        sp = 0;
    }

    public Vm(Bytecode bytecode) : this(bytecode, new IObject[GLOBALS_SIZE])
    {
    }

    Frame CurrentFrame => frames.Peek();

    public Maybe Run()
    {
        while (CurrentFrame.TryReadInstruction(out var op))
        {
            switch (op)
            {
                case Opcode.OpConstant:
                    {
                        var constIndex = CurrentFrame.ReadUInt16();
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
                        var pos = CurrentFrame.ReadUInt16();

                        var condition = Pop();
                        if (!IsTruthy(condition))
                            CurrentFrame.Jump(pos - 1);
                    }
                    break;

                case Opcode.OpJump:
                    {
                        var pos = CurrentFrame.ReadUInt16();
                        CurrentFrame.Jump(pos - 1);
                    }
                    break;

                case Opcode.OpSetGlobal:
                    {
                        var globalIndex = CurrentFrame.ReadUInt16();
                        globals[globalIndex] = Pop();
                    }
                    break;

                case Opcode.OpGetGlobal:
                    {
                        var globalIndex = CurrentFrame.ReadUInt16();
                        Push(globals[globalIndex]);
                    }
                    break;

                case Opcode.OpSetLocal:
                    {
                        var localIndex = CurrentFrame.ReadUInt8();
                        this.stack[CurrentFrame.BasePointer + localIndex] = Pop();
                    }
                    break;

                case Opcode.OpGetLocal:
                    {
                        var localIndex = CurrentFrame.ReadUInt8();
                        Push(this.stack[CurrentFrame.BasePointer + localIndex]);
                    }
                    break;

                case Opcode.OpArray:
                    {
                        var size = CurrentFrame.ReadUInt16();

                        var array = new IObject[size];
                        for (var i = size - 1; i >= 0; i--)
                            array[i] = Pop();
                        Push(new ArrayObject(array));
                    }
                    break;

                case Opcode.OpHash:
                    {
                        var size = CurrentFrame.ReadUInt16() / 2;

                        var hash = new List<(IHashable, IObject)>();
                        for (var i = 0; i < size; i++)
                        {
                            var value = Pop();
                            var key = Pop();
                            if (key is not IHashable hashable)
                                return new Exception($"unusable as hash key: {key.Type}");

                            hash.Insert(0, (hashable, value));
                        }
                        Push(new HashObject(hash.ToArray()));
                    }
                    break;

                case Opcode.OpIndex:
                    {
                        var index = Pop();
                        var left = Pop();
                        if (left is ArrayObject array && index is IntegerObject integer)
                        {
                            var result = ExecuteArrayIndex(array, integer.Value);
                            if (result.HasError)
                                return result;
                        }
                        else if (left is HashObject hash && index is IHashable hashKey)
                        {
                            var result = ExecuteHashKey(hash, hashKey);
                            if (result.HasError)
                                return result;
                        }
                        else
                        {
                            return new Exception($"index operator not supported: {left.Type}[{index.Type}]");
                        }
                    }
                    break;

                case Opcode.OpCall:
                    {
                        var fn = Pop();
                        if (fn is not CompiledFunction callable)
                            return new Exception($"calling non-function and non-built-in: {fn.Type}");

                        var frame = new Frame(callable, sp);
                        frames.Push(frame);
                        sp = frame.BasePointer + callable.NumberOfLocals;
                    }
                    break;

                case Opcode.OpReturnValue:
                    {
                        var returnValue = Pop();
                        var frame = frames.Pop();
                        this.sp = frame.BasePointer;
                        //if (frames.Count == 0)
                        //    return returnValue;
                        Push(returnValue);
                    }
                    break;

                case Opcode.OpReturn:
                    {
                        var frame = frames.Pop();
                        this.sp = frame.BasePointer;
                        Push(NullObject.Value);
                    }
                    break;
                default:
                    return new Exception($"unknown opcode {op}");
            }
        }

        return Maybe.Ok;
    }

    private Maybe ExecuteHashKey(HashObject hash, IHashable index)
    {
        if (hash.Pairs.TryGetValue(index.HashKey(), out var hashPair))
        {
            Push(hashPair.Value);
        }
        else
        {
            Push(NullObject.Value);
        }

        return Maybe.Ok;
    }

    private Maybe ExecuteArrayIndex(ArrayObject array, long value)
    {
        if (value < 0 || value >= array.Elements.Length)
        {
            Push(NullObject.Value);
            return Maybe.Ok;
        }

        var element = array.Elements[value];
        Push(element);
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
