using Microsoft.Extensions.Logging.Abstractions;
using MyCompiler.Entities;
using Xunit.Abstractions;

namespace MyCompiler.Tests
{
    public class Parser_
    {
        private readonly ITestOutputHelper outputHelper;

        public Parser_(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void Parses_let_statements()
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            string source = """
                let x = 5;
                let y = 10;
                let foobar = 838383;
                """;

            Parser parser = new(Lexer.ParseTokens(source), logger);

            var program = parser.ParseProgram();

            Assert.True(program.IsSuccess);

            Assert.Collection(program.Value.Statements,
                s => Assert.Equal(Tokens.Let, s.Token.Type),
                s => Assert.Equal(Tokens.Let, s.Token.Type),
                s => Assert.Equal(Tokens.Let, s.Token.Type)
            );
        }

        [Fact]
        public void Parses_return_statements()
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            string source = """
                return 5;
                return 10;
                return 993322;
                """;

            Parser parser = new(Lexer.ParseTokens(source), logger);

            var program = parser.ParseProgram();

            Assert.True(program.IsSuccess);

            Assert.Collection(program.Value.Statements,
                s => Assert.Equal(Tokens.Return, s.Token.Type),
                s => Assert.Equal(Tokens.Return, s.Token.Type),
                s => Assert.Equal(Tokens.Return, s.Token.Type)
            );
        }

        [Fact]
        public void Parses_expression_identifier()
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            string source = """
                        foobar;
                        57;
                        """;

            Parser parser = new(Lexer.ParseTokens(source), logger);

            var program = parser.ParseProgram();

            Assert.True(program.IsSuccess);

            Assert.Collection(program.Value.Statements,
                s => {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var id = Assert.IsType<Identifier>(es.Expression);
                    Assert.Equal("foobar", id.Value);
                },
                s => {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var number = Assert.IsType<IntegerLiteral>(es.Expression);
                    Assert.Equal(57L, number.Value);
                }
            );

        }
    }
}