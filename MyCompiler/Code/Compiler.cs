using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler.Code;

public class Compiler
{
    private readonly List<byte> _instructions = new();
    private readonly SymbolTable _symbolTable;
    private readonly List<IObject> _constants;

    EmittedInstruction _lastInstruction = default;
    EmittedInstruction _prevInstruction = default;


    public Compiler()
    {
        _symbolTable = new();
        _constants = new();
    }

    public Compiler(SymbolTable symbolTable, List<IObject> constants)
    {
        _symbolTable = symbolTable;
        _constants = constants;
    }

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

            case IntegerLiteral integerLiteral:
                {
                    var integer = new IntegerObject(integerLiteral.Value);
                    var constantIndex = AddConstant(integer);
                    Emit(Opcode.OpConstant, constantIndex);
                }
                break;

            case StringLiteral stringLiteral:
                {
                    var str = new StringObject(stringLiteral.Value);
                    var constantIndex = AddConstant(str);
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

            case ArrayExpression arrayExpression:
                {
                    var result = CompileArrayExpression(arrayExpression);
                    if (result.HasError)
                        return result;
                }
                break;

            case HashLiteral hashLiteral:
                {
                    var result = CompileHashLiteral(hashLiteral);
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
            case LetStatement letStatement:
                {
                    var result = Compile(letStatement.Expression);
                    if (result.HasError)
                        return result;

                    var symbol = _symbolTable.Define(letStatement.Identifier.Name);
                    if (symbol.HasError)
                        return symbol;

                    var emitted = Emit(Opcode.OpSetGlobal, symbol.Value.Index);
                    if (emitted.HasError)
                        return emitted;
                }
                break;
            case Identifier identifier:
                {
                    var symbol = _symbolTable.Resolve(identifier.Name);
                    if (symbol.HasError)
                        return symbol;

                    var emitted = Emit(Opcode.OpGetGlobal, symbol.Value.Index);
                    if (emitted.HasError)
                        return emitted;
                }
                break;
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

    private Maybe CompileArrayExpression(ArrayExpression arrayExpression)
    {
        foreach (var element in arrayExpression.Elements)
        {
            var result = Compile(element);
            if (result.HasError)
                return result;
        }

        return Emit(Opcode.OpArray, arrayExpression.Elements.Length);
    }

    private Maybe CompileHashLiteral(HashLiteral hashLiteral)
    {
        foreach (var pair in hashLiteral.Pairs)
        {
            var result = Compile(pair.Key);
            if (result.HasError)
                return result;

            result = Compile(pair.Value);
            if (result.HasError)
                return result;
        }

        return Emit(Opcode.OpHash, hashLiteral.Pairs.Count * 2);
    }

    private Maybe CompileIfExpression(IfExpression ifExpression)
    {
        var condition = Compile(ifExpression.Condition);
        if (condition.HasError)
            return condition;

        var jumpNotTruthyPosition = Emit(Opcode.OpJumpNotTruthy, 9999);
        if (jumpNotTruthyPosition.HasError)
            return jumpNotTruthyPosition;

        var consequence = Compile(ifExpression.Consequence);
        if (consequence.HasError)
            return consequence;

        if (_lastInstruction.Opcode == Opcode.OpPop)
            RemoveLastPop();

        var jumpFromConsequencePosition = Emit(Opcode.OpJump, 9998);
        if (jumpFromConsequencePosition.HasError)
            return jumpFromConsequencePosition;

        var afterConsequencePosition = _instructions.Count;
        ReplaceInstruction(
            jumpNotTruthyPosition.Value,
            Code.Make(Opcode.OpJumpNotTruthy, afterConsequencePosition).Value
        );

        if (ifExpression.Alternative == null)
        {
            Emit(Opcode.OpNull);
        }
        else
        {
            var alternative = Compile(ifExpression.Alternative);
            if (alternative.HasError)
                return alternative;

            if (_lastInstruction.Opcode == Opcode.OpPop)
                RemoveLastPop();
        }

        int afterAlternativePosition = _instructions.Count;
        ReplaceInstruction(
            jumpFromConsequencePosition.Value,
            Code.Make(Opcode.OpJump, afterAlternativePosition).Value
        );


        return Maybe.Ok;
    }

    private void ReplaceInstruction(int position, Span<byte> newInstruction)
    {
        for (var i = 0; i < newInstruction.Length; i++)
            _instructions[position + i] = newInstruction[i];
    }

    private void RemoveLastPop()
    {
        _instructions.RemoveAt(_instructions.Count - 1);
        _lastInstruction = _prevInstruction;
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
        _prevInstruction = _lastInstruction;
        _lastInstruction = new EmittedInstruction(opcode, pos);
    }

    private int AddInstruction(byte[] ins)
    {
        var posNewInstruction = this._instructions.Count;
        this._instructions.AddRange(ins);
        return posNewInstruction;
    }

    private int AddConstant(IObject integer)
    {
        this._constants.Add(integer);
        return this._constants.Count - 1;
    }

    public Bytecode Bytecode() => new(_instructions.ToArray(), _constants.ToArray());
}
