using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler.Code;

public class Compiler
{
    private List<byte> Instructions = new();
    private List<IObject> Constants = new();

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
                }
                break;

            case InfixExpression infixExpression:
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
                        default:
                            return new Exception($"unknown operator: {infixExpression.Operator}");
                    }
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
            //    case BooleanNode booleanNode:
            //        CompileBoolean(booleanNode);
            //        break;
            //    case PrefixNode prefixNode:
            //        CompilePrefix(prefixNode);
            //        break;
            //    case IfNode ifNode:
            //        CompileIf(ifNode);
            //        break;
            //    case BlockNode blockNode:
            //        CompileBlock(blockNode);
            //        break;
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

    private Maybe<int> Emit(Opcode opcode, params int[] operands)
    {
        var ins = Code.Make(opcode, operands);
        if (ins.HasError)
            return ins.Error!;

        var pos = AddInstruction(ins.Value);
        return pos;
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
