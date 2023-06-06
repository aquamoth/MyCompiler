using System.Diagnostics;

namespace MyCompiler.Entities;

public interface IAstNode
{

}

public interface IAstStatement : IAstNode
{
    public Token Token { get; init; }
}

public class AstProgram : IAstNode
{
    public List<IAstStatement> Statements = new();
}

[DebuggerDisplay("Empty")]
public readonly struct EmptyStatement : IAstStatement
{
    public Token Token { get; init; }
}

[DebuggerDisplay("Let {Identifier} = {Expression}")]
public readonly struct LetStatement : IAstStatement
{
    public Token Token { get; init; }
    public Identifier Identifier { get; init; }
    public IExpression Expression { get; init; }

    public override string ToString()
    {
        return $"let {Identifier} = {Expression}";
    }
}

[DebuggerDisplay("Return {ReturnValue}")]
public readonly struct ReturnStatement : IAstStatement
{
    public Token Token { get; init; }
    public IExpression ReturnValue { get; init; }
}

[DebuggerDisplay("ExpressionStatement {Expression}")]
public readonly struct ExpressionStatement : IAstStatement
{
    public Token Token { get; init; }
    public IExpression Expression { get; init; }
    override public string ToString() => $"({Expression})";
}

[DebuggerDisplay("BlockStatement {Statements}")]
public readonly struct BlockStatement : IAstStatement
{
    public Token Token { get; init; }
    public List<IAstStatement> Statements { get; init; }

    public override string ToString() => string.Join(";", Statements);
}
