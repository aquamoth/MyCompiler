using MyCompiler.Entities;

namespace MyCompiler.Tests
{
    public class Lexer_
    {
        [Fact]
        public void Parses_special_characters()
        {
            Assert.Collection(Lexer.ParseTokens("=+(){}[],;:"),
                t => Assert.Equal(t, new Token(Tokens.Assign, "=", 0, 1, 1, 1)),
                t => Assert.Equal(t, new Token(Tokens.Plus, "+", 1, 1, 1, 2)),
                t => Assert.Equal(t, new Token(Tokens.LParen, "(", 2, 1, 1, 3)),
                t => Assert.Equal(t, new Token(Tokens.RParen, ")", 3, 1, 1, 4)),
                t => Assert.Equal(t, new Token(Tokens.LSquirly, "{", 4, 1, 1, 5)),
                t => Assert.Equal(t, new Token(Tokens.RSquirly, "}", 5, 1, 1, 6)),
                t => Assert.Equal(t, new Token(Tokens.LBracket, "[", 6, 1, 1, 7)),
                t => Assert.Equal(t, new Token(Tokens.RBracket, "]", 7, 1, 1, 8)),
                t => Assert.Equal(t, new Token(Tokens.Comma, ",", 8, 1, 1, 9)),
                t => Assert.Equal(t, new Token(Tokens.Semicolon, ";", 9, 1, 1, 10)),
                t => Assert.Equal(t, new Token(Tokens.Colon, ":", 10, 1, 1, 11)),
                t => Assert.Equal(t, new Token(Tokens.EndOfFile, "", 11, 0, 1, 12))
            );
        }

        [Fact]
        public void Identifies_illegal_characters()
        {
            Assert.Collection(Lexer.ParseTokens("#"),
                t => Assert.Equal(t, new Token(Tokens.Illegal, "#", 0, 1, 1, 1)),
                t => Assert.Equal(t, new Token(Tokens.EndOfFile, "", 1, 0, 1, 2))
            );
        }

        [Fact]
        public void Parses_comparison_operators()
        {
            const string testInput = """
                                !-/*5;
                                5 < 10 > 5;
                                10 == 10;
                                10 != 9;
                                """;

            Assert.Collection(Lexer.ParseTokens(testInput),
                t => Assert.Equal(Tokens.Bang, t.Type),
                t => Assert.Equal(Tokens.Minus, t.Type),
                t => Assert.Equal(Tokens.ForwardSlash, t.Type),
                t => Assert.Equal(Tokens.Asterisk, t.Type),
                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.Semicolon, t.Type),

                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.LessThan, t.Type),
                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.GreaterThan, t.Type),
                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.Semicolon, t.Type),

                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.Equal, t.Type),
                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.Semicolon, t.Type),

                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.NotEqual, t.Type),
                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.Semicolon, t.Type),

                t => Assert.Equal(Tokens.EndOfFile, t.Type)
            );
        }

        [Fact]
        public void Parses_keywords()
        {
            const string testInput = """
                                if (5 < 10) {
                                    return true;
                                } else {
                                    return false;
                                }
                                """;

            Assert.Collection(Lexer.ParseTokens(testInput),
                t => Assert.Equal(Tokens.If, t.Type),
                t => Assert.Equal(Tokens.LParen, t.Type),
                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.LessThan, t.Type),
                t => Assert.Equal(Tokens.Integer, t.Type),
                t => Assert.Equal(Tokens.RParen, t.Type),
                t => Assert.Equal(Tokens.LSquirly, t.Type),

                t => Assert.Equal(Tokens.Return, t.Type),
                t => Assert.Equal(Tokens.True, t.Type),
                t => Assert.Equal(Tokens.Semicolon, t.Type),

                t => Assert.Equal(Tokens.RSquirly, t.Type),
                t => Assert.Equal(Tokens.Else, t.Type),
                t => Assert.Equal(Tokens.LSquirly, t.Type),

                t => Assert.Equal(Tokens.Return, t.Type),
                t => Assert.Equal(Tokens.False, t.Type),
                t => Assert.Equal(Tokens.Semicolon, t.Type),

                t => Assert.Equal(Tokens.RSquirly, t.Type),

                t => Assert.Equal(Tokens.EndOfFile, t.Type)
            );
        }

        [Fact]
        public void Parses_string()
        {
            const string testInput = """
                                "string 1"
                                "string 2"
                                "string
                                    3"
                                ""
                                """;

            Assert.Collection(Lexer.ParseTokens(testInput),
                t => Assert.Equal(Tokens.String, t.Type),
                t => Assert.Equal(Tokens.String, t.Type),
                t => Assert.Equal(Tokens.String, t.Type),
                t => Assert.Equal(Tokens.String, t.Type),
                t => Assert.Equal(Tokens.EndOfFile, t.Type)
            );
        }

        [Fact]
        public void Parses_simple_program()
        {
            const string testInput = """
                                let five = 5;
                                let ten = 10;
                                let add = fn(x, y) {
                                    x + y;
                                };
                                let result = add(five, ten);
                                """;

            Assert.Collection(Lexer.ParseTokens(testInput),
                t => Assert.Equal(new Token(Tokens.Let, "let", 0, 3, 1, 1), t),

                t => Assert.Equal(new Token(Tokens.Identifier, "five", 4, 4, 1, 5), t),
                t => Assert.Equal(new Token(Tokens.Assign, "=", 9, 1, 1, 10), t),
                t => Assert.Equal(new Token(Tokens.Integer, "5", 11, 1, 1, 12), t),
                t => Assert.Equal(new Token(Tokens.Semicolon, ";", 12, 1, 1, 13), t),

                t => Assert.Equal(new Token(Tokens.Let, "let", 15, 3, 2, 1), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "ten", 19, 3, 2, 5), t),
                t => Assert.Equal(new Token(Tokens.Assign, "=", 23, 1, 2, 9), t),
                t => Assert.Equal(new Token(Tokens.Integer, "10", 25, 2, 2, 11), t),
                t => Assert.Equal(new Token(Tokens.Semicolon, ";", 27, 1, 2, 13), t),

                t => Assert.Equal(new Token(Tokens.Let, "let", 30, 3, 3, 1), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "add", 34, 3, 3, 5), t),
                t => Assert.Equal(new Token(Tokens.Assign, "=", 38, 1, 3, 9), t),
                t => Assert.Equal(new Token(Tokens.Function, "fn", 40, 2, 3, 11), t),
                t => Assert.Equal(new Token(Tokens.LParen, "(", 42, 1, 3, 13), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "x", 43, 1, 3, 14), t),
                t => Assert.Equal(new Token(Tokens.Comma, ",", 44, 1, 3, 15), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "y", 46, 1, 3, 17), t),
                t => Assert.Equal(new Token(Tokens.RParen, ")", 47, 1, 3, 18), t),
                t => Assert.Equal(new Token(Tokens.LSquirly, "{", 49, 1, 3, 20), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "x", 56, 1, 4, 5), t),
                t => Assert.Equal(new Token(Tokens.Plus, "+", 58, 1, 4, 7), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "y", 60, 1, 4, 9), t),
                t => Assert.Equal(new Token(Tokens.Semicolon, ";", 61, 1, 4, 10), t),
                t => Assert.Equal(new Token(Tokens.RSquirly, "}", 64, 1, 5, 1), t),
                t => Assert.Equal(new Token(Tokens.Semicolon, ";", 65, 1, 5, 2), t),

                t => Assert.Equal(new Token(Tokens.Let, "let", 68, 3, 6, 1), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "result", 72, 6, 6, 5), t),
                t => Assert.Equal(new Token(Tokens.Assign, "=", 79, 1, 6, 12), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "add", 81, 3, 6, 14), t),
                t => Assert.Equal(new Token(Tokens.LParen, "(", 84, 1, 6, 17), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "five", 85, 4, 6, 18), t),
                t => Assert.Equal(new Token(Tokens.Comma, ",", 89, 1, 6, 22), t),
                t => Assert.Equal(new Token(Tokens.Identifier, "ten", 91, 3, 6, 24), t),
                t => Assert.Equal(new Token(Tokens.RParen, ")", 94, 1, 6, 27), t),
                t => Assert.Equal(new Token(Tokens.Semicolon, ";", 95, 1, 6, 28), t),

                t => Assert.Equal(new Token(Tokens.EndOfFile, "", 96, 0, 6, 29), t)
            );
        }
    }
}