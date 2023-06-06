using MyCompiler.Entities;
using Xunit.Abstractions;

namespace MyCompiler.Tests
{
    public class Interpreter_
    {
        private readonly ITestOutputHelper outputHelper;

        public Interpreter_(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Theory]
        [InlineData("5", 5)]
        public void Evaluates_integer_expressions(string source, long expected)
        {
            using var logger = new XUnitLogger<Interpreter>(outputHelper);
            var tokenSource = Lexer.ParseTokens(source);
            var program = new Parser(tokenSource).ParseProgram();

            var result = new Interpreter().Eval(program.Value);

            Assert.True(result.IsSuccess);
            var integerObject = Assert.IsType<IntegerObject>(result.Value);
            Assert.Equal(expected, integerObject.Value);
        }
    }
}