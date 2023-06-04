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

    public override string ToString() => $"{Value}";
}

[DebuggerDisplay("{Value,nq}")]
public readonly struct IntegerLiteral : IExpression
{
    public Token Token { get; init; }
    public long Value { get; init; }

    public override string ToString() => $"{Value}";
}

[DebuggerDisplay("({Operator,nq}{Right})")]
public readonly struct PrefixExpression : IExpression
{
    public Token Token { get; init; }
    public string Operator { get; init; }
    public IExpression Right { get; init; }

    public override string ToString() => $"({Operator}{Right})";
}

[DebuggerDisplay("({Left}{Operator,nq}{Right})")]
public readonly struct InfixExpression : IExpression
{
    public Token Token { get; init; }
    public IExpression Left { get; init; }
    public string Operator { get; init; }
    public IExpression Right { get; init; }

    public override string ToString() => $"({Left}{Operator}{Right})";
}
