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

        var success = compiler.Compile(program.Value);
        Assert.True(success);

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
            //var op0 = Code.Code.Make(Opcode.OpConstant, new[] { 0 }).Value;
            //var op1 = Code.Code.Make(Opcode.OpConstant, new[] { 1 }).Value;
            return new()
            {
                {
                    "1 + 2",
                    """
                        0000 OpConstant 0
                        0003 OpConstant 1
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
