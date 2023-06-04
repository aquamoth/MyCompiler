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

        [Theory]
        [InlineData("let x = 5;", "x", "5")]
        [InlineData("let y = 10;", "y", "10")]
        [InlineData("let foobar = 838383;", "foobar", "838383")]
        [InlineData("let hyped = 2*bar-foo;", "hyped", "((2*bar)-foo)")]
        public void Parses_let_statements(string source, string identifier, string expression)
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            Parser parser = new(Lexer.ParseTokens(source), logger);

            var program = parser.ParseProgram();

            Assert.True(program.IsSuccess);

            Assert.Collection(program.Value.Statements,
                s => {
                    var rs = Assert.IsType<LetStatement>(s);
                    Assert.Equal(identifier, rs.Identifier.Value);
                    Assert.Equal(expression, rs.Expression.ToString());
                }
            );
        }

        [Theory]
        [InlineData("return 5;", "5")]
        [InlineData("return 10;", "10")]
        [InlineData("return 993322;", "993322")]
        public void Parses_return_statements(string source, string expected)
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            Parser parser = new(Lexer.ParseTokens(source), logger);

            var program = parser.ParseProgram();

            Assert.True(program.IsSuccess);

            Assert.Collection(program.Value.Statements,
                s => { 
                    var rs = Assert.IsType<ReturnStatement>(s);
                    Assert.Equal(expected, rs.ReturnValue.ToString());
                }
            );
        }

        [Fact]
        public void Parses_expression_identifier_and_number()
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

        [Theory]
        [InlineData("-5", "(-5)")]
        [InlineData("!foobar", "(!foobar)")]

        [InlineData("5 + 6", "(5+6)")]
        [InlineData("5 - 6", "(5-6)")]
        [InlineData("5 * 6", "(5*6)")]
        [InlineData("5 / 6", "(5/6)")]
        [InlineData("5 > 6", "(5>6)")]
        [InlineData("5 < 6", "(5<6)")]
        [InlineData("5 == 6", "(5==6)")]
        [InlineData("5 != 6", "(5!=6)")]

        [InlineData("-a * b", "((-a)*b)")]
        [InlineData("!-a", "(!(-a))")]
        [InlineData("a + b + c", "((a+b)+c)")]
        [InlineData("a + b - c", "((a+b)-c)")]
        [InlineData("a * b * c", "((a*b)*c)")]
        [InlineData("a * b / c", "((a*b)/c)")]
        [InlineData("a + b / c", "(a+(b/c))")]
        [InlineData("a + b * c + d / e - f", "(((a+(b*c))+(d/e))-f)")]
        [InlineData("3 + 4; -5 * 5", "(3+4)", "((-5)*5)")]
        [InlineData("5 > 4 == 3 < 4", "((5>4)==(3<4))")]
        [InlineData("5 < 4 != 3 > 4", "((5<4)!=(3>4))")]
        [InlineData("3 + 4 * 5 == 3 * 1 + 4 * 5", "((3+(4*5))==((3*1)+(4*5)))")]
        public void Parses_expressions(string source, params string[] expected)
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            Parser parser = new(Lexer.ParseTokens(source), logger);

            var program = parser.ParseProgram();

            Assert.True(program.IsSuccess);
            Assert.Equal(expected.Length, program.Value.Statements.Count);

            foreach (var (actualStatement, expectedStatement) in program.Value.Statements.Zip(expected))
            {
                var es = Assert.IsType<ExpressionStatement>(actualStatement);
                Assert.Equal(expectedStatement, es.Expression.ToString());
            }
        }
    }
}