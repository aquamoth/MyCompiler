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
        [InlineData("let y = true;", "y", "true")]
        [InlineData("let foobar = y;", "foobar", "y")]
        [InlineData("let hyped = 2*bar-foo;", "hyped", "((2*bar)-foo)")]
        public void Parses_let_statements(string source, string identifier, string expression)
        {
            Assert.Collection(Parse(source),
                s =>
                {
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
            Assert.Collection(Parse(source),
                s =>
                {
                    var rs = Assert.IsType<ReturnStatement>(s);
                    Assert.Equal(expected, rs.ReturnValue.ToString());
                }
            );
        }

        [Fact]
        public void Parses_expression_identifier_and_number()
        {
            string source = """
                        foobar;
                        57;
                        true;
                        false;
                        "foobar"
                        """;

            Assert.Collection(Parse(source),
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var id = Assert.IsType<Identifier>(es.Expression);
                    Assert.Equal("foobar", id.Value);
                },
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var number = Assert.IsType<IntegerLiteral>(es.Expression);
                    Assert.Equal(57L, number.Value);
                },
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var boolean = Assert.IsType<BooleanLiteral>(es.Expression);
                    Assert.True(boolean.Value);
                },
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var boolean = Assert.IsType<BooleanLiteral>(es.Expression);
                    Assert.False(boolean.Value);
                },
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var str = Assert.IsType<StringLiteral>(es.Expression);
                    Assert.Equal("foobar", str.Value);
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

        [InlineData("3 > 5 == false", "((3>5)==false)")]
        [InlineData("3 < 5 == true", "((3<5)==true)")]
        [InlineData("!true", "(!true)")]
        [InlineData("!false", "(!false)")]

        [InlineData("1 + (2 + 3) + 4", "((1+(2+3))+4)")]
        [InlineData("(5 + 5) * 2", "((5+5)*2)")]
        [InlineData("2 / (5 + 5)", "(2/(5+5))")]
        [InlineData("-(5 + 5)", "(-(5+5))")]
        [InlineData("!(true == true)", "(!(true==true))")]

        [InlineData("a + add(b * c) + d", "((a+add((b*c)))+d)")]
        [InlineData("add(a, b, 1, 2 * 3, 4 + 5, add(6, 7 * 8))", "add(a,b,1,(2*3),(4+5),add(6,(7*8)))")]
        [InlineData("add(a + b + c * d / f + g)", "add((((a+b)+((c*d)/f))+g))")]
        public void Parses_expressions(string source, params string[] expected)
        {
            var statements = Parse(source);

            Assert.Equal(expected.Length, statements.Count);

            foreach (var (actualStatement, expectedStatement) in statements.Zip(expected))
            {
                var es = Assert.IsType<ExpressionStatement>(actualStatement);
                Assert.Equal(expectedStatement, es.Expression.ToString());
            }
        }

        [Theory]
        [InlineData("if (x < y) { x }", "(x<y)", "(x)", "")]
        [InlineData("if (x < y) { x } else { y }", "(x<y)", "(x)", "(y)")]
        public void Parses_if_expressions(string source, string condition, string consequence, string alternative)
        {
            Assert.Collection(Parse(source),
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var rs = Assert.IsType<IfExpression>(es.Expression);
                    Assert.Equal(condition, rs.Condition.ToString());
                    Assert.Equal(consequence, rs.Consequence.ToString());
                    Assert.Equal(alternative, rs.Alternative.ToString());
                }
            );
        }

        [Theory]
        [InlineData("fn(x, y) { x + y; }", "((x+y))", "x", "y")]
        [InlineData("fn() {};", "")]
        [InlineData("fn(x) { let a=0; 123 }", "let a = 0;(123)", "x")]
        public void Parses_functions(string source, string expectedBody, params string[] expectedParams)
        {
            Assert.Collection(Parse(source),
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var rs = Assert.IsType<FnExpression>(es.Expression);

                    Assert.Equal(expectedParams.Length, rs.Parameters.Length);
                    foreach(var (expected, actual) in expectedParams.Zip(rs.Parameters))
                        Assert.Equal(expected, actual.Value);

                    Assert.Equal(expectedBody, rs.Body.ToString());
                }
            );
        }

        [Theory]
        [InlineData("add()", "add()")]
        [InlineData("add(1);", "add(1)")]
        [InlineData("add(1, 2 * 3, 4 + 5);", "add(1,(2*3),(4+5))")]
        public void Parses_function_calls(string source, string expected)
        {
            Assert.Collection(Parse(source),
                s =>
                {
                    var es = Assert.IsType<ExpressionStatement>(s);
                    var rs = Assert.IsType<CallExpression>(es.Expression);
                    Assert.Equal(expected, rs.ToString());
                }
            );
        }


        private List<IAstStatement> Parse(string source)
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            Parser parser = new(Lexer.ParseTokens(source), logger);
            var program = parser.ParseProgram();
            Assert.True(program.IsSuccess);

            return program.Value.Statements;
        }
    }
}