using MyCompiler.Entities;

namespace MyCompiler;

public static class Lexer
{
    public static IEnumerable<Token> ParseTokens(string input)
    {
        var position = 0;
        var lineNumber = 1;
        var lineStart = 0;

        while (position < input.Length)
        {
            if (char.IsWhiteSpace(input[position]))
            {
                if (input[position] == '\n')
                {
                    lineNumber++;
                    lineStart = position + 1;
                }

                position++;
                continue;
            }

            var ch = input[position];
            var start = position;
            position++;

            if (char.IsDigit(ch))
            {
                while (char.IsDigit(input.Peek(position)))
                {
                    position++;
                }

                var word = input[start..position];
                yield return Token.From(Tokens.Integer, word, start, position, lineNumber, lineStart);
                continue;
            }


            if (char.IsLetter(ch))
            {
                while (char.IsLetterOrDigit(input.Peek(position)))
                {
                    position++;
                }

                var word = input[start..position];
                var token = word switch
                {
                    "let" => Tokens.Let,
                    "fn" => Tokens.Function,
                    "if" => Tokens.If,
                    "else" => Tokens.Else,
                    "return" => Tokens.Return,
                    "true" => Tokens.True,
                    "false" => Tokens.False,
                    _ => Tokens.Identifier
                };
                yield return Token.From(token, word, start, position, lineNumber, lineStart);
                continue;
            }

            if (ch == '"')
            {
                while (position < input.Length && input.Peek(position) != '"')
                {
                    if (input.Peek(position) == '\n')
                    {
                        lineNumber++;
                        lineStart = position + 1;
                    }

                    position++;
                }

                if (position < input.Length)
                    position++;

                var word = input[start..position];
                yield return Token.From(Tokens.String, word, start, position, lineNumber, lineStart);
                continue;
            }

            if (ch == '=')
            {
                if (input.Peek(position) == '=')
                {
                    position += 1;
                    yield return Token.From(Tokens.Equal, "==", start, position, lineNumber, lineStart);
                    continue;
                }
            }

            if (ch == '!')
            {
                if (input.Peek(position) == '=')
                {
                    position += 1;
                    yield return Token.From(Tokens.NotEqual, "!=", start, position, lineNumber, lineStart);
                    continue;
                }
            }

            {
                var token = ch switch
                {
                    '=' => Tokens.Assign,
                    '+' => Tokens.Plus,
                    '-' => Tokens.Minus,
                    '/' => Tokens.ForwardSlash,
                    '!' => Tokens.Bang,
                    '*' => Tokens.Asterisk,
                    '<' => Tokens.LessThan,
                    '>' => Tokens.GreaterThan,
                    '(' => Tokens.LParen,
                    ')' => Tokens.RParen,
                    '{' => Tokens.LSquirly,
                    '}' => Tokens.RSquirly,
                    ',' => Tokens.Comma,
                    ';' => Tokens.Semicolon,
                    _ => Tokens.Illegal
                };
                yield return Token.From(token, ch.ToString(), start, position, lineNumber, lineStart);
            }
        }

        yield return Token.From(Tokens.EndOfFile, "", position, position, lineNumber, lineStart);
    }

    private static char Peek(this string input, int position)
    {
        if (position >= input.Length)
        {
            return '\0';
        }
        return input[position];
    }
}
