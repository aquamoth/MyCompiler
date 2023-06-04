using System.Diagnostics;

namespace MyCompiler.Entities;

public interface IExpression
{
}

[DebuggerDisplay("{Value,nq}")]
public readonly struct Identifier : IExpression
{
    public Token Token { get; init; }
    public string Value { get; init; }
}

[DebuggerDisplay("{Value,nq}")]
public readonly struct IntegerLiteral : IExpression
{
    public Token Token { get; init; }
    public long Value { get; init; }
}



//[DebuggerDisplay("[expression]")]
//public readonly struct Expression
//{
//    public Expression Left { get; init; }
//    public Token Operator { get; init; }
//    public Expression Right { get; init; }
//}