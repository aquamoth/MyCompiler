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
        var success = compiler.Compile(program.Value);
        Assert.True(success);

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
                { "1 + 2", new IntegerObject(2) } // TODO: FIXME
            };
        }
    }

    //[Fact]
    //public void Runs_()
    //{
    //    var compiler = new Compiler();
    //    var program = Parse("1 + 2");
    //    var success = compiler.Compile(program.Value);
    //    Assert.True(success);
    //    var bytecode = compiler.Bytecode();
    //    var vm = new Vm(bytecode);
    //    vm.Run();
    //    var stackTop = vm.StackTop();
    //    Assert.Equal(1, stackTop.Type);
    //}

    private Helpers.Maybe<AstProgram> Parse(string Input)
    {
        var lexer = Lexer.ParseTokens(Input);
        var parser = new Parser(lexer, new XUnitLogger<Parser>(outputHelper));
        return parser.ParseProgram();
    }
}