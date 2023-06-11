using MyCompiler.Code;
using MyCompiler.Entities;
using Xunit.Abstractions;

namespace MyCompiler.Tests;

public class Compiler_
{
    private readonly ITestOutputHelper outputHelper;

    public Compiler_(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
    }

    [Theory]
    [MemberData(nameof(Compiles_integer_arithmetic_VALUES))]
    public void Compiles_integer_arithmetic(string input, string expectedInstructions, object[] expectedConstants)
    {
        var compiler = new Compiler();
        var program = Parse(input);

        var compilation = compiler.Compile(program.Value);
        Assert.False(compilation.HasError);

        var bytecode = compiler.Bytecode();

        var actualInstructions = Code.Code.Disassemble(bytecode.Instructions);
        Assert.True(actualInstructions.HasValue);

        Assert.Equal(expectedInstructions, actualInstructions.Value);
        Assert.Equal(expectedConstants, bytecode.Constants);
    }
    public static TheoryData<string, string, object[]> Compiles_integer_arithmetic_VALUES
    {
        get
        {
            return new()
            {
                {
                    "1 + 2",
                    """
                        0000 OpConstant 0
                        0003 OpConstant 1
                        0006 OpAdd
                        """,
                    new object[]{
                        new IntegerObject { Value = 1 },
                        new IntegerObject { Value = 2 }
                    }
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
