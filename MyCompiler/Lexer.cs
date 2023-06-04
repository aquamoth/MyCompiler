namespace MyCompiler;

public static partial class Lexer
{
    public static IEnumerable<Token> ParseTokens(string input)
    {
        var position = 0;
        var lineNumber = 1;
        var lineStart = 0;

        while (position < input.Length)
        {
            var ch = input[position];

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


            if (char.IsDigit(ch))
            {
                var start = position;
                do
                {
                    position++;
                }
                while (position < input.Length && char.IsDigit(input[position]));

                //var word = input[start..position];
                yield return Token.From(Tokens.Integer, start, position, lineNumber, lineStart);
                continue;
            }


            if (char.IsLetter(ch))
            {
                var start = position;
                do
                {
                    position++;
                }
                while (position < input.Length && char.IsLetterOrDigit(input[position]));

                var word = input[start..position];
                var token = word switch
                {
                    "let" => Tokens.Let,
                    "fn" => Tokens.Function,
                    _ => Tokens.Identifier
                };
                yield return Token.From(token, start, position, lineNumber, lineStart);
                continue;
            }


            {
                var token = ch switch
                {
                    '=' => Tokens.Equal,
                    '+' => Tokens.Plus,
                    '(' => Tokens.LParen,
                    ')' => Tokens.RParen,
                    '{' => Tokens.LSquirly,
                    '}' => Tokens.RSquirly,
                    ',' => Tokens.Comma,
                    ';' => Tokens.Semicolon,
                    _ => Tokens.Illegal
                };
                yield return Token.From(token, position, position + 1, lineNumber, lineStart);
            }

            position++;
        }

        yield return Token.From(Tokens.EndOfFile, position, position, lineNumber, lineStart);
    }
}
