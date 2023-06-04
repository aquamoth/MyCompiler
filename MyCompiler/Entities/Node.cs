namespace MyCompiler.Entities;

public interface IAstNode
{
    public Token Token { get; init; }
}

public class AstProgram
{
    public List<IAstNode> Statements = new();
}

public readonly struct EmptyStatement : IAstNode
{
    public Token Token { get; init; }
}

public readonly struct LetStatement : IAstNode
{
    public Token Token { get; init; }
    public Identifier Identifier { get; init; }
}

public readonly struct ReturnStatement : IAstNode
{
    public Token Token { get; init; }
    public IAstNode ReturnValue { get; init; }
}

public readonly struct Identifier : IAstNode
{
    public Token Token { get; init; }
    public string Name { get; init; }
}

public readonly struct Expression : IAstNode
{
    public Token Token { get; init; }
}

//public readonly struct OperatorExpression : AstNode
//{
//    public Node Left { get; init; }
//    public Token Operator { get; init; }
//    public Node Right { get; init; }
//}