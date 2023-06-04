namespace MyCompiler
{
    public enum Tokens
    {
        Illegal,

        Assign,
        Equal,
        NotEqual,
        Plus,
        LParen,
        RParen,
        LSquirly,
        RSquirly,
        Comma,
        Semicolon,

        Bang,
        Minus,
        ForwardSlash,
        Asterisk,
        LessThan,
        GreaterThan,

        Let,
        Identifier,
        Integer,
        Function,
        True,
        False,
        If,
        Else,
        Return,

        EndOfFile
    }
}
