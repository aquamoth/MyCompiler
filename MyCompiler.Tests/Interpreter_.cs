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
        [InlineData("-5", -5)]
        [InlineData("10", 10)]
        [InlineData("-10", -10)]

        [InlineData("5 + 5 + 5 + 5 - 10", 10)]
        [InlineData("2 * 2 * 2 * 2 * 2", 32)]
        [InlineData("-50 + 100 + -50", 0)]
        [InlineData("5 * 2 + 10", 20)]
        [InlineData("5 + 2 * 10", 25)]
        [InlineData("20 + 2 * -10", 0)]
        [InlineData("50 / 2 * 2 + 10", 60)]
        [InlineData("2 * (5 + 10)", 30)]
        [InlineData("3 * 3 * 3 + 10", 37)]
        [InlineData("3 * (3 * 3) + 10", 37)]
        [InlineData("(5 + 10 * 2 + 15 / 3) * 2 + -10", 50)]
        public void Evaluates_integer_expressions(string source, long expected)
        {
            var integerObject = Assert.IsType<IntegerObject>(Interpret(source));
            Assert.Equal(expected, integerObject.Value);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]

        [InlineData("1 < 2", true)]
        [InlineData("1 > 2", false)]
        [InlineData("1 < 1", false)]
        [InlineData("1 > 1", false)]
        [InlineData("1 == 1", true)]
        [InlineData("1 != 1", false)]
        [InlineData("1 == 2", false)]
        [InlineData("1 != 2", true)]

        [InlineData("true == true", true)]
        [InlineData("false == false", true)]
        [InlineData("true == false", false)]
        [InlineData("true != true", false)]
        [InlineData("false != false", false)]
        [InlineData("true != false", true)]

        [InlineData("(1 < 2) == true", true)]
        [InlineData("(1 < 2) == false", false)]
        [InlineData("(1 > 2) == true", false)]
        [InlineData("(1 > 2) == false", true)]
        public void Evaluates_boolean_expressions(string source, bool expected)
        {
            var booleanObject = Assert.IsType<BooleanObject>(Interpret(source));
            Assert.Equal(expected, booleanObject.Value);
        }

        [Theory]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("!5", false)]
        [InlineData("!!true", true)]
        [InlineData("!!false", false)]
        [InlineData("!!5", true)]
        //[InlineData("!0", true)]
        public void Evaluates_bang_expressions(string source, bool expected)
        {
            var booleanObject = Assert.IsType<BooleanObject>(Interpret(source));
            Assert.Equal(expected, booleanObject.Value);
        }

        [Theory]
        [InlineData("if (true) { 10 }", 10)]
        [InlineData("if (1) { 10 }", 10)]
        [InlineData("if (1 < 2) { 10 }", 10)]
        [InlineData("if (1 > 2) { 10 } else { 20 }", 20)]
        [InlineData("if (1 < 2) { 10 } else { 20 }", 10)]
        public void Evaluates_if_expressions(string source, long expected)
        {
            var integerObject = Assert.IsType<IntegerObject>(Interpret(source));
            Assert.Equal(expected, integerObject.Value);
        }

        [Theory]
        [InlineData("if (false) { 10 }")]
        [InlineData("if (1 > 2) { 10 }")]
        public void Evaluates_if_expressions_null_output(string source)
        {
            Assert.Equal(NullObject.Value, Interpret(source));
        }

        [Theory]
        [InlineData("return 10;", 10)]
        [InlineData("return 10; 9;", 10)]
        [InlineData("return 2 * 5; 9;", 10)]
        [InlineData("9; return 2 * 5; 9;", 10)]
        [InlineData("if (10 > 1) { if (10 > 1) { return 10; } return 1; }", 10)]
        public void Returns_from_return_statements(string source, int expected)
        {
            var actual = Assert.IsType<IntegerObject>(Interpret(source));
            Assert.Equal(expected, actual.Value);
        }

        [Theory]
        [InlineData("5 + true;", "type mismatch: INTEGER + BOOLEAN")]
        [InlineData("5 + true; 5;", "type mismatch: INTEGER + BOOLEAN")]
        [InlineData("-true", "unknown operator: -BOOLEAN")]
        [InlineData("true + false;", "unknown operator: BOOLEAN + BOOLEAN")]
        [InlineData("5; true + false; 5", "unknown operator: BOOLEAN + BOOLEAN")]
        [InlineData("if (10 > 1) { true + false; }", "unknown operator: BOOLEAN + BOOLEAN")]
        [InlineData("""
                    if (10 > 1) {
                        if (10 > 1) {
                            return true + false;
                        }
                        return 1;
                    }
                    """, "unknown operator: BOOLEAN + BOOLEAN")]
        public void Handles_error_states(string source, string expectedErrorMessage)
        {
            using var logger = new XUnitLogger<Interpreter>(outputHelper);

            var tokenSource = Lexer.ParseTokens(source);
            var program = new Parser(tokenSource).ParseProgram();
            var actual = new Interpreter().Eval(program.Value);

            Assert.False(actual.IsSuccess, "Expected interpreter to fail, but it did not.");
            Assert.Equal(expectedErrorMessage, actual.Error?.Message);
        }


        private IObject Interpret(string source)
        {
            using var logger = new XUnitLogger<Interpreter>(outputHelper);
            var tokenSource = Lexer.ParseTokens(source);
            var program = new Parser(tokenSource).ParseProgram();

            var result = new Interpreter().Eval(program.Value);
            Assert.True(result.IsSuccess, result.Error?.Message);
            return result.Value;
        }
    }
}