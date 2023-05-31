namespace MyCompiler;

public record Token(Tokens Type, int Position, int Length, int Line = 0, int Column = 0);
