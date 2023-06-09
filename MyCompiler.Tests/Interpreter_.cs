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
            var integerObject = AssertInterpret<IntegerObject>(source);
            Assert.Equal(expected, integerObject.Value);
        }

        [Theory]
        [InlineData("\"foobar\"", "foobar")]
        [InlineData("\"foo\" + \"bar\"", "foobar")]
        public void Evaluates_string_expressions(string source, string expected)
        {
            var stringObject = AssertInterpret<StringObject>(source);
            Assert.Equal(expected, stringObject.Value);
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

        [InlineData(""" "abc" == "abc" """, true)]
        [InlineData(""" "abc" == "def" """, false)]
        [InlineData(""" "abc" != "abc" """, false)]
        [InlineData(""" "abc" != "def" """, true)]

        [InlineData(""" "abc" < "abc" """, false)]
        [InlineData(""" "abc" < "def" """, true)]
        [InlineData(""" "def" < "abc" """, false)]

        [InlineData(""" "abc" > "abc" """, false)]
        [InlineData(""" "abc" > "def" """, false)]
        [InlineData(""" "def" > "abc" """, true)]
        public void Evaluates_boolean_expressions(string source, bool expected)
        {
            var booleanObject = AssertInterpret<BooleanObject>(source);
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
            var booleanObject = AssertInterpret<BooleanObject>(source);
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
            var integerObject = AssertInterpret<IntegerObject>(source);
            Assert.Equal(expected, integerObject.Value);
        }

        [Theory]
        [InlineData("if (false) { 10 }")]
        [InlineData("if (1 > 2) { 10 }")]
        public void Evaluates_if_expressions_null_output(string source)
        {
            Assert.Equal(NullObject.Value, AssertInterpret<NullObject>(source));
        }

        [Theory]
        [InlineData("return 10;", 10)]
        [InlineData("return 10; 9;", 10)]
        [InlineData("return 2 * 5; 9;", 10)]
        [InlineData("9; return 2 * 5; 9;", 10)]
        [InlineData("if (10 > 1) { if (10 > 1) { return 10; } return 1; }", 10)]
        [InlineData("let add = fn(a,b){ let x=7; return 4; a+b};let x=3; let c = add(x,2); (c*2)", 8)]
        public void Returns_from_return_statements(string source, int expected)
        {
            var actual = AssertInterpret<IntegerObject>(source);
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
        [InlineData("foobar", "identifier not found: foobar")]
        public void Handles_error_states(string source, string expectedErrorMessage)
        {
            var actual = Interpret(source);

            Assert.False(actual.HasValue, "Expected interpreter to fail, but it did not.");
            Assert.Equal(expectedErrorMessage, actual.Error?.Message);
        }

        [Theory]
        [InlineData("let a = 5; a;", 5)]
        [InlineData("let a = 5 * 5; a;", 25)]
        [InlineData("let a = 5; let b = a; b;", 5)]
        [InlineData("let a = 5; let b = a; let c = a + b + 5; c;", 15)]
        public void Evaluates_let_statements(string source, long expected)
        {
            var integerObject = AssertInterpret<IntegerObject>(source);
            Assert.Equal(expected, integerObject.Value);
        }

        [Fact]
        public void Evaluates_functions()
        {
            var fnObject = AssertInterpret<FunctionObject>("fn(x) { x + 2; };");
            Assert.Collection(fnObject.Parameters,
                p => Assert.Equal("x", p.Value)
            );

            Assert.Equal("((x+2))", fnObject.Body.ToString());
        }

        [Theory]
        [InlineData("len(\"\")", 0)]
        [InlineData("len(\"four\")", 4)]
        [InlineData("len(\"hello world\")", 11)]
        [InlineData("len([])", 0)]
        [InlineData("len([1,1,1,1])", 4)]
        [InlineData("first([1,2,3,4])", 1)]
        [InlineData("last([1,2,3,4])", 4)]
        //[InlineData("len(1)", 0)]
        //[InlineData("len(\"one\", \"two\")", 0)]
        public void Builtin_functions(string source, int expected)
        {
            var integerObject = AssertInterpret<IntegerObject>(source);
            Assert.Equal(expected, integerObject.Value);
        }

        [Theory]
        [InlineData("len(1)", "Expected STRING or ARRAY but got INTEGER")]
        [InlineData("len(\"one\", \"two\")", "wrong number of arguments. got=2, want=1")]
        public void Builtin_len_asserts(string source, string error)
        {
            var integerObject = Interpret(source);
            Assert.False(integerObject.HasValue);
            Assert.Equal(error, integerObject.Error?.Message);
        }

        [Theory]
        [InlineData("rest([1,2,3,4])", "[2,3,4]")]
        [InlineData("rest([1])", "[]")]
        [InlineData("rest([])", "")]
        [InlineData("push([], 5)", "[5]")]
        [InlineData("push([1], 5)", "[1,5]")]
        [InlineData("push([1,2], 5)", "[1,2,5]")]
        public void Builtin_array_functions(string source, string expected)
        {
            var result = Interpret(source);
            Assert.True(result.HasValue);
            Assert.Equal(expected, result.Value.Inspect());
        }

        [Theory]
        [InlineData("let identity = fn(x) { x; }; identity(5);", 5)]
        [InlineData("let identity = fn(x) { return x; }; identity(5);", 5)]
        [InlineData("let double = fn(x) { x * 2; }; double(5);", 10)]
        [InlineData("let add = fn(x, y) { x + y; }; add(5, 5);", 10)]
        [InlineData("let add = fn(x, y) { x + y; }; add(5 + 5, add(5, 5));", 20)]
        [InlineData("fn(x) { x; }(5)", 5)]
        [InlineData("""
                        let newAdder = fn(x) {
                            fn(y) { x + y };
                        };
                        let addTwo = newAdder(2);
                        addTwo(2);
                    """, 4)]
        public void Calls_functions(string source, int expected)
        {
            var integerObject = AssertInterpret<IntegerObject>(source);
            Assert.Equal(expected, integerObject.Value);
        }


        [Theory]
        [InlineData("[1, 2, 3][0]", 1)]
        [InlineData("[1, 2, 3][1]", 2)]
        [InlineData("[1, 2, 3][2]", 3)]
        [InlineData("let i = 0; [1][i];", 1)]
        [InlineData("[1, 2, 3][1 + 1];", 3)]
        [InlineData("let myArray = [1, 2, 3]; myArray[2];", 3)]
        [InlineData("let myArray = [1, 2, 3]; myArray[0] + myArray[1] + myArray[2];", 6)]
        [InlineData("let myArray = [1, 2, 3]; let i = myArray[0]; myArray[i]", 2)]
        //[InlineData("[1, 2, 3][3]", nil)]
        //[InlineData("[1, 2, 3][-1]", nil)]
        public void Evaluates_Index_expressions(string source, int expected)
        {
            var integerObject = AssertInterpret<IntegerObject>(source);
            Assert.Equal(expected, integerObject.Value);
        }


        [Theory]
        [InlineData("""{"name": "William", "age": 48};""", """{ "name": "William", "age": 48 }""")]
        [InlineData("{}", "{  }")]
        public void Evaluates_Hash_literals(string source, string expected)
        {
            var hashObject = AssertInterpret<HashObject>(source);
            Assert.Equal(expected, hashObject.Inspect());
        }

        [Theory]
        [InlineData("""{"name": "William", "age": 48}["age"];""", "48")]
        [InlineData("""{}["foo"];""", "")]
        [InlineData("""let key = "foo"; {"foo": 5}[key];""", "5")]
        [InlineData("""{5:5}[5];""", "5")]
        [InlineData("""{true:5}[true];""", "5")]
        [InlineData("""{false:5}[false];""", "5")]
        public void Evaluates_Hash_Indexes(string source, string expected)
        {
            var hashObject = Interpret(source);
            Assert.True(hashObject.HasValue);
            Assert.Equal(expected, hashObject.Value.Inspect());
        }


        private T AssertInterpret<T>(string source) where T : IObject
        {
            var result = Interpret(source);
            Assert.True(result.HasValue, result.Error?.Message);
            return Assert.IsType<T>(result.Value);
        }

        private Maybe<IObject> Interpret(string source)
        {
            using var logger = new XUnitLogger<Interpreter>(outputHelper);
            var tokenSource = Lexer.ParseTokens(source);
            var program = new Parser(tokenSource, logger).ParseProgram();
            Assert.True(program.HasValue, "Failed to parse program!");

            var env = EnvironmentStore.New();
            Interpreter interpreter = new(logger);
            return interpreter.Eval(program.Value, env);
        }
    }
}