using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.Buffers.Binary;
using System.Runtime;

namespace MyCompiler.Code;

readonly struct EmittedInstruction
{
    public readonly Opcode Opcode { get; init; }
    public readonly int Position { get; init; }
}

public class Compiler
{
    private List<byte> Instructions = new();
    private List<IObject> Constants = new();

    EmittedInstruction lastInstruction = default;
    EmittedInstruction prevInstruction = default;

    public Maybe Compile(IAstNode node)
    {
        switch (node)
        {
            case AstProgram programNode:
                foreach (var statement in programNode.Statements)
                {
                    var result = Compile(statement);
                    if (result.HasError)
                        return result;
                }
                break;

            case ExpressionStatement expressionStatement:
                {
                    var result = Compile(expressionStatement.Expression);
                    if (result.HasError)
                        return result;

                    Emit(Opcode.OpPop);
                }
                break;

            case InfixExpression infixExpression:
                {
                    var result = CompileInfixExpression(infixExpression);
                    if (result.HasError)
                        return result;
                }
                break;

            //    case StatementNode statementNode:
            //        CompileStatement(statementNode);
            //        break;
            case IntegerLiteral integerLiteral:
                {
                    var integer = new IntegerObject { Value = integerLiteral.Value };
                    var constantIndex = AddConstant(integer);
                    Emit(Opcode.OpConstant, constantIndex);
                }
                break;

            case BooleanLiteral booleanLiteral:
                Emit(booleanLiteral.Value ? Opcode.OpTrue : Opcode.OpFalse);
                break;

            case PrefixExpression prefixExpression:
                {
                    var result = CompilePrefixExpression(prefixExpression);
                    if (result.HasError)
                        return result;
                }
                break;

            case IfExpression ifExpression:
                {
                    var result = CompileIfExpression(ifExpression);
                    if (result.HasError)
                        return result;
                }
                break;

            case BlockStatement blockStatement:
                foreach (var statement in blockStatement.Statements)
                {
                    var result = Compile(statement);
                    if (result.HasError)
                        return result;
                }
                break;

            //    case ReturnNode returnNode:
            //        CompileReturn(returnNode);
            //        break;
            //    case LetNode letNode:
            //        CompileLet(letNode);
            //        break;
            //    case IdentifierNode identifierNode:
            //        CompileIdentifier(identifierNode);
            //        break;
            //    case FunctionNode functionNode:
            //        CompileFunction(functionNode);
            //        break;
            //    case CallNode callNode:
            //        CompileCall(callNode);
            //        break;
            default:
                return new Exception($"unknown node type: {node.GetType()}");
        }

        return Maybe.Ok;
    }

    private Maybe CompileIfExpression(IfExpression ifExpression)
    {
        var condition = Compile(ifExpression.Condition);
        if (condition.HasError)
            return condition;

        var jumpNotTruthyPos = Emit(Opcode.OpJumpNotTruthy, 9999);
        if (jumpNotTruthyPos.HasError)
            return jumpNotTruthyPos.Error!;

        var consequence = Compile(ifExpression.Consequence);
        if (consequence.HasError)
            return consequence.Error!;

        if (lastInstruction.Opcode == Opcode.OpPop)
            RemoveLastPop();

        ushort newJumpTarget = (ushort)Instructions.Count;
        var newInstruction = Code.Make(Opcode.OpJumpNotTruthy, newJumpTarget).Value;
        ReplaceInstruction(jumpNotTruthyPos.Value, newInstruction);

        return Maybe.Ok;
    }

    private void ReplaceInstruction(int position, Span<byte> newInstruction)
    {
        for (var i = 0; i < newInstruction.Length; i++)
            Instructions[position + i] = newInstruction[i];
    }

    private void RemoveLastPop()
    {
        Instructions.RemoveAt(Instructions.Count - 1);
        lastInstruction = prevInstruction;
    }

    private Maybe CompileInfixExpression(InfixExpression infixExpression)
    {

        if (infixExpression.Operator == "<")
        {
            var result = Compile(infixExpression.Right);
            if (result.HasError)
                return result;

            result = Compile(infixExpression.Left);
            if (result.HasError)
                return result;

            Emit(Opcode.OpGreaterThan);
        }
        else
        {
            var result = Compile(infixExpression.Left);
            if (result.HasError)
                return result;

            result = Compile(infixExpression.Right);
            if (result.HasError)
                return result;

            switch (infixExpression.Operator)
            {
                case "+":
                    Emit(Opcode.OpAdd);
                    break;
                case "-":
                    Emit(Opcode.OpSub);
                    break;
                case "*":
                    Emit(Opcode.OpMul);
                    break;
                case "/":
                    Emit(Opcode.OpDiv);
                    break;

                case ">":
                    Emit(Opcode.OpGreaterThan);
                    break;
                case "==":
                    Emit(Opcode.OpEqual);
                    break;
                case "!=":
                    Emit(Opcode.OpNotEqual);
                    break;

                default:
                    return new Exception($"unknown operator: {infixExpression.Operator}");
            }
        }

        return Maybe.Ok;
    }

    private Maybe CompilePrefixExpression(PrefixExpression prefixExpression)
    {
        var right = Compile(prefixExpression.Right);
        if (right.HasError)
            return right;

        switch (prefixExpression.Operator)
        {
            case "-":
                Emit(Opcode.OpMinus);
                break;
            case "!":
                Emit(Opcode.OpBang);
                break;
        }

        return Maybe.Ok;
    }

    private Maybe<int> Emit(Opcode opcode, params int[] operands)
    {
        var ins = Code.Make(opcode, operands);
        if (ins.HasError)
            return ins.Error!;

        var pos = AddInstruction(ins.Value);
        SetLastInstruction(opcode, pos);

        return pos;
    }

    private void SetLastInstruction(Opcode opcode, int pos)
    {
        prevInstruction = lastInstruction;
        lastInstruction = new EmittedInstruction { Opcode = opcode, Position = pos };
    }

    private int AddInstruction(byte[] ins)
    {
        var posNewInstruction = this.Instructions.Count;
        this.Instructions.AddRange(ins);
        return posNewInstruction;
    }

    private int AddConstant(IntegerObject integer)
    {
        this.Constants.Add(integer);
        return this.Constants.Count - 1;
    }

    public Bytecode Bytecode()
    {
        return new Bytecode
        {
            Instructions = this.Instructions.ToArray(),
            Constants = this.Constants.ToArray()
        };
    }
}
