namespace MyCompiler.Tests
{
    public class Lexer_
    {
        [Fact]
        public void Parses_special_characters()
        {
            const string testInput = "=+(){},;";
            var tokens = Lexer.ParseTokens(testInput);

            var expectedResult = new List<Token>
            {
                new Token(Tokens.Equal,0,1,1,1     ),
                new Token(Tokens.Plus,1,1   ,1,2   ),
                new Token(Tokens.LParen,2,1  ,1,3  ),
                new Token(Tokens.RParen,3,1   ,1,4 ),
                new Token(Tokens.LSquirly,4,1 ,1,5 ),
                new Token(Tokens.RSquirly,5,1  ,1,6),
                new Token(Tokens.Comma,6,1     ,1,7),
                new Token(Tokens.Semicolon,7,1 ,1,8),
                new Token(Tokens.Eof,8,0       ,1,9)
            };

            Assert.Equal(expectedResult, tokens);
        }

        [Fact]
        public void Identifies_illegal_characters()
        {
            const string testInput = "#";
            var tokens = Lexer.ParseTokens(testInput);

            var expectedResult = new List<Token>
            {
                new Token(Tokens.Illegal, 0, 1,1,1),
                new Token(Tokens.Eof, 1, 0,1,2)
            };

            Assert.Equal(expectedResult, tokens);
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

            var tokens = Lexer.ParseTokens(testInput);

            Assert.Collection(tokens,
                t => Assert.Equal(new Token(Tokens.Let, 0, 3, 1, 1), t),

                t => Assert.Equal(new Token(Tokens.Identifier, 4, 4, 1, 5), t),       // five
                t => Assert.Equal(new Token(Tokens.Equal, 9, 1, 1, 10), t),        // =
                t => Assert.Equal(new Token(Tokens.Integer, 11, 1, 1, 12), t),      // 5
                t => Assert.Equal(new Token(Tokens.Semicolon, 12, 1, 1, 13), t),   // ;

                t => Assert.Equal(new Token(Tokens.Let, 15, 3, 2, 1), t),           // let
                t => Assert.Equal(new Token(Tokens.Identifier, 19, 3, 2, 5), t),    // ten
                t => Assert.Equal(new Token(Tokens.Equal, 23, 1, 2, 9), t),       // =
                t => Assert.Equal(new Token(Tokens.Integer, 25, 2, 2, 11), t),      // 10
                t => Assert.Equal(new Token(Tokens.Semicolon, 27, 1, 2, 13), t),   // ;

                t => Assert.Equal(new Token(Tokens.Let, 30, 3, 3, 1), t),           // let
                t => Assert.Equal(new Token(Tokens.Identifier, 34, 3, 3, 5), t),    // add
                t => Assert.Equal(new Token(Tokens.Equal, 38, 1, 3, 9), t),       // =
                t => Assert.Equal(new Token(Tokens.Function, 40, 2, 3, 11), t),    // fn
                t => Assert.Equal(new Token(Tokens.LParen, 42, 1, 3, 13), t),      // (
                t => Assert.Equal(new Token(Tokens.Identifier, 43, 1, 3, 14), t),  // x
                t => Assert.Equal(new Token(Tokens.Comma, 44, 1, 3, 15), t),       // ,
                t => Assert.Equal(new Token(Tokens.Identifier, 46, 1, 3, 17), t),  // y
                t => Assert.Equal(new Token(Tokens.RParen, 47, 1, 3, 18), t),      // )
                t => Assert.Equal(new Token(Tokens.LSquirly, 49, 1, 3, 20), t),    // {
                t => Assert.Equal(new Token(Tokens.Identifier, 56, 1, 4, 5), t),  // x
                t => Assert.Equal(new Token(Tokens.Plus, 58, 1, 4, 7), t),        // +
                t => Assert.Equal(new Token(Tokens.Identifier, 60, 1, 4, 9), t),  // y
                t => Assert.Equal(new Token(Tokens.Semicolon, 61, 1, 4, 10), t),   // ;
                t => Assert.Equal(new Token(Tokens.RSquirly, 64, 1, 5, 1), t),    // }
                t => Assert.Equal(new Token(Tokens.Semicolon, 65, 1, 5, 2), t),   // ;

                t => Assert.Equal(new Token(Tokens.Let, 68, 3, 6, 1), t),         // let
                t => Assert.Equal(new Token(Tokens.Identifier, 72, 6, 6, 5), t),  // result
                t => Assert.Equal(new Token(Tokens.Equal, 79, 1, 6, 12), t),       // =
                t => Assert.Equal(new Token(Tokens.Identifier, 81, 3, 6, 14), t),  // add
                t => Assert.Equal(new Token(Tokens.LParen, 84, 1, 6, 17), t),      // (
                t => Assert.Equal(new Token(Tokens.Identifier, 85, 4, 6, 18), t),  // five
                t => Assert.Equal(new Token(Tokens.Comma, 89, 1, 6, 22), t),       // ,
                t => Assert.Equal(new Token(Tokens.Identifier, 91, 3, 6, 24), t),  // ten
                t => Assert.Equal(new Token(Tokens.RParen, 94, 1, 6, 27), t),      // )
                t => Assert.Equal(new Token(Tokens.Semicolon, 95, 1, 6, 28), t),   // ;

                t => Assert.Equal(new Token(Tokens.Eof, 96, 0, 6, 29), t)
            );
        }
    }
}