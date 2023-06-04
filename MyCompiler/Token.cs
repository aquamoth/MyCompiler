namespace MyCompiler;

public record Token(Tokens Type, int Position, int Length, int Line = 0, int Column = 0)
{
    public static Token From(Tokens type, int startPosition, int endPosition, int lineNumber, int startOfLine)
    {
        return new Token(type, startPosition, endPosition - startPosition, lineNumber, startPosition - startOfLine + 1);
    }
}
