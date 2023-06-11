using MyCompiler.Code;
using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.Text;
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
                    "1;2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0).Value,
                        Code.Code.Make(Opcode.OpPop).Value,
                        Code.Code.Make(Opcode.OpConstant, 1).Value,
                        Code.Code.Make(Opcode.OpPop).Value
                    ),
                    new object[]{
                        new IntegerObject { Value = 1 },
                        new IntegerObject { Value = 2 }
                    }
                },
                {
                    "1 + 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0).Value,
                        Code.Code.Make(Opcode.OpConstant, 1).Value,
                        Code.Code.Make(Opcode.OpAdd).Value,
                        Code.Code.Make(Opcode.OpPop).Value
                    ),
                    new object[]{
                        new IntegerObject { Value = 1 },
                        new IntegerObject { Value = 2 }
                    }
                },
                {
                    "1 - 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0).Value,
                        Code.Code.Make(Opcode.OpConstant, 1).Value,
                        Code.Code.Make(Opcode.OpSub).Value,
                        Code.Code.Make(Opcode.OpPop).Value
                    ),
                    new object[]{
                        new IntegerObject { Value = 1 },
                        new IntegerObject { Value = 2 }
                    }
                },
                {
                    "1 * 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0).Value,
                        Code.Code.Make(Opcode.OpConstant, 1).Value,
                        Code.Code.Make(Opcode.OpMul).Value,
                        Code.Code.Make(Opcode.OpPop).Value
                    ),
                    new object[]{
                        new IntegerObject { Value = 1 },
                        new IntegerObject { Value = 2 }
                    }
                },
                {
                    "2 / 1",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0).Value,
                        Code.Code.Make(Opcode.OpConstant, 1).Value,
                        Code.Code.Make(Opcode.OpDiv).Value,
                        Code.Code.Make(Opcode.OpPop).Value
                    ),
                    new object[]{
                        new IntegerObject { Value = 2 },
                        new IntegerObject { Value = 1 }
                    }
                },
            };
        }
    }

    private static string Disassemble(params byte[][] operations) => Code.Code.Disassemble(operations.SelectMany(x => x).ToArray()).Value;

    private Helpers.Maybe<AstProgram> Parse(string Input)
    {
        var lexer = Lexer.ParseTokens(Input);
        var parser = new Parser(lexer, new XUnitLogger<Parser>(outputHelper));
        return parser.ParseProgram();
    }
}
