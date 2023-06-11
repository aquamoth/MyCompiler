using MyCompiler.Entities;

namespace MyCompiler.Code;

public class Compiler
{
    private List<byte> Instructions = new();
    private List<object> Constants = new();

    public bool Compile(IAstNode node)
    {
        switch (node)
        {
            case AstProgram programNode:
                foreach (var statement in programNode.Statements)
                {
                    if (!Compile(statement))
                        return false;
                }
                break;
            case ExpressionStatement expressionStatement:
                if (!Compile(expressionStatement.Expression))
                    return false;
                break;
            case InfixExpression infixExpression:
                if (!Compile(infixExpression.Left))
                    return false;
                if (!Compile(infixExpression.Right))
                    return false;
                break;
            //    case StatementNode statementNode:
            //        CompileStatement(statementNode);
            //        break;
            case IntegerLiteral integerLiteral:
                var integer = new IntegerObject { Value = integerLiteral.Value };
                var constantIndex = AddConstant(integer);
                Emit(Opcode.OpConstant, constantIndex);
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
                throw new Exception($"unknown node type: {node.GetType()}");
        }
        return true;
    }

    private int Emit(Opcode opcode, params int[] operands)
    {
        var ins = Code.Make(opcode, operands);
        if (ins.HasError) throw new Exception(ins.Error!.Message);

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
