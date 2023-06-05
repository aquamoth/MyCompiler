using System.Diagnostics;

namespace MyCompiler.Entities;

public interface IAstStatement
{
    public Token Token { get; init; }
}

public class AstProgram
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
}

[DebuggerDisplay("Return {ReturnValue}")]
public readonly struct ReturnStatement : IAstStatement
{
    public Token Token { get; init; }
    public IExpression ReturnValue { get; init; }
}

[DebuggerDisplay("{Expression}")]
public readonly struct ExpressionStatement : IAstStatement
{
    public Token Token { get; init; }
    public IExpression Expression { get; init; }
    override public string ToString() => $"({Expression})";
}

//[DebuggerDisplay("{Expression}")]
public readonly struct BlockStatement : IAstStatement
{
    public Token Token { get; init; }
    public List<IAstStatement> Statements { get; init; }

    public override string ToString() => string.Join(";", Statements);
}
