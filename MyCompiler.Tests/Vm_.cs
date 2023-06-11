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
    [MemberData(nameof(Runs_integer_arithmetic_VALUES))]
    public void Runs_integer_arithmetic(string source, IObject expectedStackTop)
    {
        var program = Parse(source);

        var compiler = new Compiler();
        var compilation = compiler.Compile(program.Value);
        Assert.False(compilation.HasError);

        var vm = new Vm.Vm(compiler.Bytecode());
        vm.Run();

        var stackTop = vm.StackTop();
        Assert.True(stackTop.HasValue);

        Assert.Equal(expectedStackTop, stackTop.Value);
    }
    public static TheoryData<string, IObject> Runs_integer_arithmetic_VALUES
    {
        get
        {
            return new()
            {
                {"1", new IntegerObject(1) },
                {"2", new IntegerObject(2) },
                { "1 + 2", new IntegerObject(3) }
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