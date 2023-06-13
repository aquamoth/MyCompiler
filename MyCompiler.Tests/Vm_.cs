using MyCompiler.Code;
using MyCompiler.Entities;
using Xunit.Abstractions;

namespace MyCompiler.Tests;

public class Vm_
{
    private readonly ITestOutputHelper outputHelper;

    public Vm_(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
    }

    [Theory]
    [MemberData(nameof(Runs_bytecode_INTEGERS))]
    [MemberData(nameof(Runs_bytecode_BOOLEANS))]
    [MemberData(nameof(Runs_bytecode_CONDITIONALS))]
    [MemberData(nameof(Runs_bytecode_GLOBALS))]
    [MemberData(nameof(Runs_bytecode_STRINGS))]
    [MemberData(nameof(Runs_bytecode_ARRAYS))]
    [MemberData(nameof(Runs_bytecode_HASHES))]
    public void Runs_bytecode(string source, IObject expectedStackTop)
    {
        var program = Parse(source);

        var compiler = new Compiler();
        var compilation = compiler.Compile(program.Value);
        Assert.False(compilation.HasError, compilation.Error?.Message);

        var vm = new Vm.Vm(compiler.Bytecode());
        var computation = vm.Run();
        Assert.False(computation.HasError, computation.Error?.Message);

        var stackElement = vm.LastPoppedStackElem();
        Assert.Equal(expectedStackTop, stackElement);
    }
    public static TheoryData<string, IObject> Runs_bytecode_INTEGERS
    {
        get
        {
            return new()
            {
                {"1", new IntegerObject(1) },
                {"2", new IntegerObject(2) },

                { "1 + 2", new IntegerObject(3) },
                { "1 - 2", new IntegerObject(-1) },
                { "1 * 2", new IntegerObject(2) },
                { "4 / 2", new IntegerObject(2) },
                { "50 / 2 * 2 + 10 - 5", new IntegerObject(55) },
                { "5 + 5 + 5 + 5 - 10", new IntegerObject(10) },
                { "2 * 2 * 2 * 2 * 2", new IntegerObject(32) },
                { "5 * 2 + 10", new IntegerObject(20) },
                { "5 + 2 * 10", new IntegerObject(25) },
                { "5 * (2 + 10)", new IntegerObject(60) },

                { "-5", new IntegerObject(-5) },
                { "-10", new IntegerObject(-10) },
                { "-50 + 100 + -50", new IntegerObject(0) },
                { "(5 + 10 * 2 + 15 / 3) * 2 + -10", new IntegerObject(50) }
            };
        }
    }
    public static TheoryData<string, IObject> Runs_bytecode_BOOLEANS
    {
        get
        {
            return new()
            {
                {"true", BooleanObject.True },
                {"false", BooleanObject.False},

                {"1 < 2", BooleanObject.True},
                {"1 > 2", BooleanObject.False},
                {"1 < 1", BooleanObject.False},
                {"1 > 1", BooleanObject.False},
                {"1 == 1", BooleanObject.True},
                {"1 != 1", BooleanObject.False},
                {"1 == 2", BooleanObject.False},
                {"1 != 2", BooleanObject.True},

                {"true == true", BooleanObject.True},
                {"false == false", BooleanObject.True},
                {"true == false", BooleanObject.False},
                {"true != false", BooleanObject.True},
                {"false != true", BooleanObject.True},

                {"(1 < 2) == true", BooleanObject.True},
                {"(1 < 2) == false", BooleanObject.False},
                {"(1 > 2) == true", BooleanObject.False},
                {"(1 > 2) == false", BooleanObject.True},

                {"!true", BooleanObject.False},
                {"!false", BooleanObject.True},
                {"!5", BooleanObject.False},
                {"!!true", BooleanObject.True},
                {"!!false", BooleanObject.False},
                {"!!5", BooleanObject.True},

                //{"!(if (false) { 5; })", BooleanObject.True},{"(1 > 2) == false", BooleanObject.True},
            };
        }
    }
    public static TheoryData<string, IObject> Runs_bytecode_CONDITIONALS
    {
        get
        {
            return new()
            {
                {"if (true) { 10 }", new IntegerObject(10)},
                {"if (true) { 10 } else { 20 }", new IntegerObject(10)},
                {"if (false) { 10 } else { 20 }", new IntegerObject(20)},
                {"if (1) { 10 }", new IntegerObject(10)},
                {"if (1 < 2) { 10 }", new IntegerObject(10)},
                {"if (1 < 2) { 10 } else { 20 }", new IntegerObject(10)},
                {"if (1 > 2) { 10 } else { 20 }", new IntegerObject(20)},
                {"if (1 > 2) { 10 }", NullObject.Value},
                {"!(if (false) { 5; })", BooleanObject.True},
                {"if((if (false) { 10 })) { 10 } else { 20 }", new IntegerObject(20)},
            };
        }
    }
    public static TheoryData<string, IObject> Runs_bytecode_GLOBALS
    {
        get
        {
            return new()
            {
                {"let one = 1; one;", new IntegerObject(1)},
                {"let one = 1; let two = 2; one + two;", new IntegerObject(3)},
                {"let one = 1; let two = one + one; one + two;", new IntegerObject(3)},
            };
        }
    }
    public static TheoryData<string, IObject> Runs_bytecode_STRINGS
    {
        get
        {
            return new()
            {
                {
                    """
                    "monkey"
                    """,
                    new StringObject("monkey")
                },
                {
                    """
                    "mon" + "key"
                    """,
                    new StringObject("monkey")
                },
                {
                    """
                    "mon" + "key" + "banana"
                    """,
                    new StringObject("monkeybanana")
                },
            };
        }
    }
    public static TheoryData<string, IObject> Runs_bytecode_ARRAYS
    {
        get
        {
            return new()
            {
                {
                    "[]",
                    new ArrayObject(Array.Empty<IObject>())
                },
                {
                    "[1, 2, 3]",
                    new ArrayObject(new[]{ 1,2,3}.Select(x=>new IntegerObject(x)).Cast<IObject>().ToArray())
                },
                {
                    "[1 + 2, 3 * 4, 5 + 6]",
                    new ArrayObject(new[]{ 3,12,11}.Select(x=>new IntegerObject(x)).Cast<IObject>().ToArray())
                },
            };
        }
    }
    public static TheoryData<string, IObject> Runs_bytecode_HASHES
    {
        get
        {
            return new()
            {
                {
                    "{}",
                    new HashObject()
                },
                {
                    "{1: 2, 2: 3}",
                    new HashObject(
                        (new IntegerObject(1), new IntegerObject(2)),
                        (new IntegerObject(2), new IntegerObject(3))
                    )
                },
                {
                    "{1 + 1: 2 * 2, 3 + 3: 4 * 4}",
                    new HashObject(
                        (new IntegerObject(2), new IntegerObject(4)),
                        (new IntegerObject(6), new IntegerObject(16))
                    )
                },
            };
        }
    }

    private Helpers.Maybe<AstProgram> Parse(string Input)
    {
        var lexer = Lexer.ParseTokens(Input);
        var parser = new Parser(lexer, new XUnitLogger<Parser>(outputHelper));
        return parser.ParseProgram();
    }
}