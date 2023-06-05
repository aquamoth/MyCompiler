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

[DebuggerDisplay("{Value,nq}")]
public readonly struct BooleanLiteral : IExpression
{
    public Token Token { get; init; }
    public bool Value { get; init; }

    public override string ToString() => Value ? "true" : "false";
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

[DebuggerDisplay("if {Condition,nq}")]
public readonly struct IfExpression : IExpression
{
    public Token Token { get; init; }
    public IExpression Condition { get; init; }
    public BlockStatement Consequence { get; init; }
    public BlockStatement? Alternative { get; init; }

    //public override string ToString() => $"if {Condition}\nthen {Consequence}\nelse {Alternative}";
}

[DebuggerDisplay("fn({Parameters,nq}) {Body,nq}")]
public readonly struct FnExpression : IExpression
{
    public Token Token { get; init; }
    public Identifier[] Parameters { get; init; }
    public BlockStatement Body { get; init; }

    //public override string ToString() => $"if {Condition}\nthen {Consequence}\nelse {Alternative}";
}

[DebuggerDisplay("{Function,nq}({Arguments,nq})")]
public readonly struct CallExpression : IExpression
{
    public Token Token { get; init; }
    public IExpression Function { get; init; }
    public IExpression[] Arguments { get; init; }

    public override string ToString() => $"{Function}({string.Join<IExpression>(",", Arguments)})";
}
