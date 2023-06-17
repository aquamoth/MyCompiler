using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler.Code;

public class Compiler
{
    internal SymbolTable _symbolTable;
    private readonly List<IObject> _constants;

    private readonly Stack<CompilationScope> Scopes = new();
    internal int ScopeIndex => Scopes.Count - 1;
    internal CompilationScope CurrentScope => Scopes.Peek();

    public Compiler()
    {
        _symbolTable = new();
        _constants = new();
        Scopes.Push(new CompilationScope());
    }

    public Compiler(SymbolTable symbolTable, List<IObject> constants)
    {
        _symbolTable = symbolTable;
        _constants = constants;
        Scopes.Push(new CompilationScope());
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

            case IndexExpression indexExpression:
                {
                    var result = CompileIndexExpression(indexExpression);
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

            case LetStatement letStatement:
                {
                    var result = Compile(letStatement.Expression);
                    if (result.HasError)
                        return result;

                    var symbol = _symbolTable.Define(letStatement.Identifier.Name);
                    if (symbol.HasError)
                        return symbol;

                    var opcode = symbol.Value.Scope == Symbol.LOCAL_SCOPE
                        ? Opcode.OpSetLocal
                        : Opcode.OpSetGlobal;

                    var emitted = Emit(opcode, symbol.Value.Index);
                    if (emitted.HasError)
                        return emitted;
                }
                break;

            case Identifier identifier:
                {
                    var symbol = _symbolTable.Resolve(identifier.Name);
                    if (symbol.HasError)
                        return symbol;

                    var opcode = symbol.Value.Scope == Symbol.LOCAL_SCOPE
                        ? Opcode.OpGetLocal
                        : Opcode.OpGetGlobal;

                    var emitted = Emit(opcode, symbol.Value.Index);
                    if (emitted.HasError)
                        return emitted;
                }
                break;

            case FunctionLiteral functionLiteral:
                {
                    var result = CompileFunction(functionLiteral);
                    if (result.HasError)
                        return result;
                }
                break;

            case ReturnStatement returnStatement:
                {
                    var result = Compile(returnStatement.ReturnValue);
                    if (result.HasError)
                        return result;

                    var emitted = Emit(Opcode.OpReturnValue);
                    if (emitted.HasError)
                        return emitted;
                }
                break;

            case CallExpression callExpression:
                {
                    var result = Compile(callExpression.Function);
                    if (result.HasError)
                        return result;

                    foreach (var argument in callExpression.Arguments)
                    {
                        result = Compile(argument);
                        if (result.HasError)
                            return result;
                    }

                    Emit(Opcode.OpCall, callExpression.Arguments.Length);
                }
                break;

            default:
                return new Exception($"unknown node type: {node.GetType()}");
        }

        return Maybe.Ok;
    }

    private Maybe CompileFunction(FunctionLiteral functionLiteral)
    {
        EnterScope();

        foreach (var p in functionLiteral.Parameters)
        {
            var symbol = _symbolTable.Define(p.Name);
            if (symbol.HasError)
                return symbol;
        }

        var compiled = Compile(functionLiteral.Body);
        if (compiled.HasError)
            return compiled;

        CurrentScope.ReplaceLastPopWithReturn();

        if (CurrentScope.LastInstruction.Opcode != Opcode.OpReturnValue)
        {
            Emit(Opcode.OpReturn);
        }

        var numberOfLocals = _symbolTable.store.Count;
        var instructions = LeaveScope();

        var fn = new CompiledFunction(instructions, numberOfLocals, functionLiteral.Parameters.Length);
        return Emit(Opcode.OpConstant, AddConstant(fn));
    }

    private Maybe CompileIndexExpression(IndexExpression indexExpression)
    {
        var result = Compile(indexExpression.Left);
        if (result.HasError)
            return result;

        result = Compile(indexExpression.Index);
        if (result.HasError)
            return result;

        var emitted = Emit(Opcode.OpIndex);
        if (emitted.HasError)
            return emitted;

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

        if (CurrentScope.LastInstruction.Opcode == Opcode.OpPop)
            CurrentScope.RemoveLastPop();

        var jumpFromConsequencePosition = Emit(Opcode.OpJump, 9998);
        if (jumpFromConsequencePosition.HasError)
            return jumpFromConsequencePosition;

        var afterConsequencePosition = CurrentScope.Instructions.Count;
        CurrentScope.ReplaceInstruction(
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

            if (CurrentScope.LastInstruction.Opcode == Opcode.OpPop)
                CurrentScope.RemoveLastPop();
        }

        int afterAlternativePosition = CurrentScope.Instructions.Count;
        CurrentScope.ReplaceInstruction(
            jumpFromConsequencePosition.Value,
            Code.Make(Opcode.OpJump, afterAlternativePosition).Value
        );


        return Maybe.Ok;
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

    internal Maybe<int> Emit(Opcode opcode, params int[] operands)
    {
        var ins = Code.Make(opcode, operands);
        if (ins.HasError)
            return ins.Error!;

        var pos = CurrentScope.AddInstruction(ins.Value);
        CurrentScope.SetLastInstruction(opcode, pos);

        return pos;
    }

    private int AddConstant(IObject integer)
    {
        this._constants.Add(integer);
        return this._constants.Count - 1;
    }

    public Bytecode Bytecode() => new(CurrentScope.Instructions.ToArray(), _constants.ToArray());

    internal void EnterScope()
    {
        Scopes.Push(new CompilationScope());
        _symbolTable = new SymbolTable(_symbolTable);
    }

    internal byte[] LeaveScope()
    {
        var oldScope = Scopes.Pop();
        _symbolTable = _symbolTable.Outer ?? throw new Exception("Expected outer symbolTable when leaving scope");
        return oldScope.Instructions.ToArray();
    }
}
