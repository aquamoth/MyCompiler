using System.Diagnostics;

namespace MyCompiler.Entities;

public interface IExpression : IAstNode
{
    public Token Token { get; }
}

[DebuggerDisplay("Identifier {Value,nq}")]
public readonly struct Identifier : IExpression
{
    public Token Token { get; init; }
    public string Value { get; init; }

    public override string ToString() => $"{Value}";
}

[DebuggerDisplay("IntegerLiteral {Value,nq}")]
public readonly struct IntegerLiteral : IExpression
{
    public Token Token { get; init; }
    public long Value { get; init; }

    public override string ToString() => $"{Value}";
}

[DebuggerDisplay("StringLiteral {Value,nq}")]
public readonly struct StringLiteral : IExpression
{
    public Token Token { get; init; }
    public string Value { get; init; }

    public override string ToString() => $"\"{Value}\"";
}

[DebuggerDisplay("BooleanLiteral {Value,nq}")]
public readonly struct BooleanLiteral : IExpression
{
    public Token Token { get; init; }
    public bool Value { get; init; }

    public override string ToString() => Value ? "true" : "false";
}

[DebuggerDisplay("prefix ({Operator,nq}{Right})")]
public readonly struct PrefixExpression : IExpression
{
    public Token Token { get; init; }
    public string Operator { get; init; }
    public IExpression Right { get; init; }

    public override string ToString() => $"({Operator}{Right})";
}

[DebuggerDisplay("infix ({Left}{Operator,nq}{Right})")]
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

    public override string ToString() => $"fn({string.Join(",", Parameters)}){{{Body}}}";
}

[DebuggerDisplay("call {Function,nq}({Arguments,nq})")]
public readonly struct CallExpression : IExpression
{
    public Token Token { get; init; }
    public IExpression Function { get; init; }
    public IExpression[] Arguments { get; init; }

    public override string ToString() => $"{Function}({string.Join<IExpression>(",", Arguments)})";
}

[DebuggerDisplay("[{Values,nq}]")]
public readonly struct ArrayExpression : IExpression
{
    public Token Token { get; init; }
    public IExpression[] Elements { get; init; }

    public override string ToString() => $"[{string.Join<IExpression>(",", Elements)}]";
}


[DebuggerDisplay("index {Array,nq}({Index,nq})")]
public readonly struct IndexExpression : IExpression
{
    public Token Token { get; init; }
    public IExpression Left { get; init; }
    public IExpression Right { get; init; }

    public override string ToString() => $"({Left}[{Right}])";
}

//[DebuggerDisplay("{{{Pairs,nq}}}")]
public readonly struct HashLiteral : IExpression
{
    public Token Token { get; init; }
    public IDictionary<IExpression, IExpression> Pairs { get; init; }

    public override string ToString() => $"{{{string.Join(",", Pairs.Select(p=>$"{p.Key}:{p.Value}"))}}}";

    public HashLiteral(Token token)
    {
        Token = token;
        Pairs = new Dictionary<IExpression, IExpression>();
    }
}