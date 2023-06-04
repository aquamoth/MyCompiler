namespace MyCompiler.Entities;

public record Token(Tokens Type, string Literal, int Position, int Length, int Line = 0, int Column = 0)
{
    public static Token From(Tokens type, string literal, int startPosition, int endPosition, int lineNumber, int startOfLine)
    {
        return new Token(type, literal, startPosition, endPosition - startPosition, lineNumber, startPosition - startOfLine + 1);
    }
}
