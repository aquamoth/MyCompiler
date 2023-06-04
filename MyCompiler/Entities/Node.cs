namespace MyCompiler.Entities;

public interface Node
{

}

public class ProgramNode
{
    public List<Node> Statements = new();
}

public readonly struct LetStatement : Node
{
    public Token Token { get; init; }
    public IdentifierNode Identifier { get; init; }
}

public readonly struct IdentifierNode : Node
{
    public Token Token { get; init; }
    public string Name { get; init; }
}

//public readonly struct OperatorExpression : Node
//{
//    public Node Left { get; init; }
//    public Token Operator { get; init; }
//    public Node Right { get; init; }
//}