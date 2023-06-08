namespace MyCompiler.Entities;

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
    LBracket,
    RBracket,
    Comma,
    Semicolon,
    Colon,

    Bang,
    Minus,
    ForwardSlash,
    Asterisk,
    LessThan,
    GreaterThan,

    Let,
    Identifier,
    Integer,
    String,
    Function,
    True,
    False,
    If,
    Else,
    Return,

    EndOfFile
}
