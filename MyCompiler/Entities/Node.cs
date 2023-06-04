using System.Diagnostics;
using System.Runtime.CompilerServices;

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

[DebuggerDisplay("Let {Identifier} = ?")]
public readonly struct LetStatement : IAstStatement
{
    public Token Token { get; init; }
    public Identifier Identifier { get; init; }
}

[DebuggerDisplay("Return {ReturnValue}")]
public readonly struct ReturnStatement : IAstStatement
{
    public Token Token { get; init; }
    public IAstStatement ReturnValue { get; init; }
}

[DebuggerDisplay("{Expression}")]
public readonly struct ExpressionStatement : IAstStatement
{
    public Token Token { get; init; }
    public Expression Expression { get; init; }
}

[DebuggerDisplay("{Name,nq}")]
public readonly struct Identifier
{
    public Token Token { get; init; }
    public string Name { get; init; }
}

[DebuggerDisplay("[expression]")]
public readonly struct Expression
{

}

//public readonly struct OperatorExpression : AstNode
//{
//    public Node Left { get; init; }
//    public Token Operator { get; init; }
//    public Node Right { get; init; }
//}