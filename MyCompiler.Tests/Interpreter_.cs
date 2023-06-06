using MyCompiler.Entities;
using MyCompiler.Helpers;
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
        [InlineData("98", 98)]
        public void Evaluates_integer_expressions(string source, long expected)
        {
            var integerObject = Assert.IsType<IntegerObject>(Interpret(source));
            Assert.Equal(expected, integerObject.Value);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void Evaluates_boolean_expressions(string source, bool expected)
        {
            var booleanObject = Assert.IsType<BooleanObject>(Interpret(source));
            Assert.Equal(expected, booleanObject.Value);
        }

        private IObject Interpret(string source)
        {
            using var logger = new XUnitLogger<Interpreter>(outputHelper);
            var tokenSource = Lexer.ParseTokens(source);
            var program = new Parser(tokenSource).ParseProgram();

            var result = new Interpreter().Eval(program.Value);
            Assert.True(result.IsSuccess);
            return result.Value;
        }
    }
}