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
                yield return new Token(Tokens.Integer, start, position - start, lineNumber, start - lineStart + 1);
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
                yield return new Token(token, start, position - start, lineNumber, start - lineStart + 1);
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
                yield return new Token(token, position, 1, lineNumber, position - lineStart + 1);
            }

            position++;
        }

        yield return new Token(Tokens.Eof, position, 0, lineNumber, position - lineStart + 1);
    }
}
