﻿using MyCompiler.Code;
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
    [MemberData(nameof(Compiles_source_to_bytecode_INTEGERS))]
    [MemberData(nameof(Compiles_source_to_bytecode_BOOLEANS))]
    [MemberData(nameof(Compiles_source_to_bytecode_CONDITIONALS))]
    [MemberData(nameof(Compiles_source_to_bytecode_GLOBALS))]
    [MemberData(nameof(Compiles_source_to_bytecode_STRINGS))]
    [MemberData(nameof(Compiles_source_to_bytecode_ARRAYS))]
    [MemberData(nameof(Compiles_source_to_bytecode_HASHES))]
    public void Compiles_source_to_bytecode(string input, string expectedInstructions, object[] expectedConstants)
    {
        var compiler = new Compiler();
        var program = Parse(input);
        Assert.True(program.HasValue, program.Error?.Message);

        var compilation = compiler.Compile(program.Value);
        Assert.False(compilation.HasError, compilation.Error?.Message);

        var bytecode = compiler.Bytecode();

        var actualInstructions = Code.Code.Disassemble(bytecode.Instructions);
        Assert.True(actualInstructions.HasValue, actualInstructions.Error?.Message);

        Assert.Equal(expectedInstructions, actualInstructions.Value);
        Assert.Equal(expectedConstants, bytecode.Constants);
    }
    public static TheoryData<string, string, object[]> Compiles_source_to_bytecode_INTEGERS
        => new()
            {
                {
                    "1;2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpPop),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2)
                    }
                },
                {
                    "1 + 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpAdd),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2)
                    }
                },
                {
                    "1 - 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpSub),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2)
                    }
                },
                {
                    "1 * 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpMul),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2)
                    }
                },
                {
                    "2 / 1",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpDiv),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(2),
                        new IntegerObject(1)
                    }
                },
                {
                    "-1",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpMinus),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1)
                    }
                },
            };
    public static TheoryData<string, string, object[]> Compiles_source_to_bytecode_BOOLEANS
        => new()
            {
                {
                    "true",
                    Disassemble(
                        Code.Code.Make(Opcode.OpTrue),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                    }
                },
                {
                    "false",
                    Disassemble(
                        Code.Code.Make(Opcode.OpFalse),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                    }
                },
                {
                    "1 > 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpGreaterThan),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2)
                    }
                },
                {
                    "1 < 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpGreaterThan),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(2),
                        new IntegerObject(1)
                    }
                },
                {
                    "1 == 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpEqual),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2)
                    }
                },
                {
                    "1 != 2",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpNotEqual),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2)
                    }
                },
                {
                    "true == false",
                    Disassemble(
                        Code.Code.Make(Opcode.OpTrue),
                        Code.Code.Make(Opcode.OpFalse),
                        Code.Code.Make(Opcode.OpEqual),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                    }
                },
                {
                    "true != false",
                    Disassemble(
                        Code.Code.Make(Opcode.OpTrue),
                        Code.Code.Make(Opcode.OpFalse),
                        Code.Code.Make(Opcode.OpNotEqual),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                    }
                },
                {
                    "!true",
                    Disassemble(
                        Code.Code.Make(Opcode.OpTrue),
                        Code.Code.Make(Opcode.OpBang),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                    }
                },
            };
    public static TheoryData<string, string, object[]> Compiles_source_to_bytecode_CONDITIONALS
    => new()
        {
            {
                "if (true) { 10 }; 3333;",
                Disassemble(
                    Code.Code.Make(Opcode.OpTrue),
                    Code.Code.Make(Opcode.OpJumpNotTruthy, 10),
                    Code.Code.Make(Opcode.OpConstant, 0),
                    Code.Code.Make(Opcode.OpJump, 11),
                    Code.Code.Make(Opcode.OpNull),
                    Code.Code.Make(Opcode.OpPop),
                    Code.Code.Make(Opcode.OpConstant, 1),
                    Code.Code.Make(Opcode.OpPop)
                ),
                new object[]{
                    new IntegerObject(10),
                    new IntegerObject(3333)
                }
            },
            {
                "if (true) { 10 } else { 20 }; 3333;",
                Disassemble(
                    Code.Code.Make(Opcode.OpTrue),
                    Code.Code.Make(Opcode.OpJumpNotTruthy, 10),
                    Code.Code.Make(Opcode.OpConstant, 0),
                    Code.Code.Make(Opcode.OpJump, 13),
                    Code.Code.Make(Opcode.OpConstant, 1),
                    Code.Code.Make(Opcode.OpPop),
                    Code.Code.Make(Opcode.OpConstant, 2),
                    Code.Code.Make(Opcode.OpPop)
                ),
                new object[]{
                    new IntegerObject(10),
                    new IntegerObject(20),
                    new IntegerObject(3333)
                }
            },
        };
    public static TheoryData<string, string, object[]> Compiles_source_to_bytecode_GLOBALS
    => new()
        {
            {
                "let one = 1; let two = 2;",
                Disassemble(
                    Code.Code.Make(Opcode.OpConstant, 0),
                    Code.Code.Make(Opcode.OpSetGlobal, 0),
                    Code.Code.Make(Opcode.OpConstant, 1),
                    Code.Code.Make(Opcode.OpSetGlobal, 1)
                ),
                new object[]{
                    new IntegerObject(1),
                    new IntegerObject(2)
                }
            },
            {
                "let one = 1; one;",
                Disassemble(
                    Code.Code.Make(Opcode.OpConstant, 0),
                    Code.Code.Make(Opcode.OpSetGlobal, 0),
                    Code.Code.Make(Opcode.OpGetGlobal, 0),
                    Code.Code.Make(Opcode.OpPop)
                ),
                new object[]{
                    new IntegerObject(1),
                }
            },
            {
                "let one = 1; let two = one; two;",
                Disassemble(
                    Code.Code.Make(Opcode.OpConstant, 0),
                    Code.Code.Make(Opcode.OpSetGlobal, 0),
                    Code.Code.Make(Opcode.OpGetGlobal, 0),
                    Code.Code.Make(Opcode.OpSetGlobal, 1),
                    Code.Code.Make(Opcode.OpGetGlobal, 1),
                    Code.Code.Make(Opcode.OpPop)
                ),
                new object[]{
                    new IntegerObject(1),
                }
            },
        };
    public static TheoryData<string, string, object[]> Compiles_source_to_bytecode_STRINGS
        => new()
            {
                {
                    """
                    "monkey"
                    """,
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new StringObject("monkey")
                    }
                },
                {
                    """
                    "mon" + "key"
                    """,
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpAdd),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new StringObject("mon"),
                        new StringObject("key")
                    }
                },
                {
                    """
                    "mon" + "key" + "banana"
                    """,
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpAdd),
                        Code.Code.Make(Opcode.OpConstant, 2),
                        Code.Code.Make(Opcode.OpAdd),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new StringObject("mon"),
                        new StringObject("key"),
                        new StringObject("banana")
                    }
                },
            };
    public static TheoryData<string, string, object[]> Compiles_source_to_bytecode_ARRAYS
        => new()
            {
                {
                    "[]",
                    Disassemble(
                        Code.Code.Make(Opcode.OpArray, 0),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                    }
                },
                {
                    "[1, 2, 3]",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpConstant, 2),
                        Code.Code.Make(Opcode.OpArray, 3),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2),
                        new IntegerObject(3)
                    }
                },
                {
                    "[1 + 2, 3 - 4, 5 * 6]",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpAdd),
                        Code.Code.Make(Opcode.OpConstant, 2),
                        Code.Code.Make(Opcode.OpConstant, 3),
                        Code.Code.Make(Opcode.OpSub),
                        Code.Code.Make(Opcode.OpConstant, 4),
                        Code.Code.Make(Opcode.OpConstant, 5),
                        Code.Code.Make(Opcode.OpMul),
                        Code.Code.Make(Opcode.OpArray, 3),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2),
                        new IntegerObject(3),
                        new IntegerObject(4),
                        new IntegerObject(5),
                        new IntegerObject(6)
                    }
                },
            };
    public static TheoryData<string, string, object[]> Compiles_source_to_bytecode_HASHES
        => new()
            {
                {
                    "{}",
                    Disassemble(
                        Code.Code.Make(Opcode.OpHash, 0),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                    }
                },
                {
                    "{1: 2, 3: 4, 5: 6}",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpConstant, 2),
                        Code.Code.Make(Opcode.OpConstant, 3),
                        Code.Code.Make(Opcode.OpConstant, 4),
                        Code.Code.Make(Opcode.OpConstant, 5),
                        Code.Code.Make(Opcode.OpHash, 6),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2),
                        new IntegerObject(3),
                        new IntegerObject(4),
                        new IntegerObject(5),
                        new IntegerObject(6)
                    }
                },
                {
                    "{1: 2 + 3, 4: 5 * 6}",
                    Disassemble(
                        Code.Code.Make(Opcode.OpConstant, 0),
                        Code.Code.Make(Opcode.OpConstant, 1),
                        Code.Code.Make(Opcode.OpConstant, 2),
                        Code.Code.Make(Opcode.OpAdd),
                        Code.Code.Make(Opcode.OpConstant, 3),
                        Code.Code.Make(Opcode.OpConstant, 4),
                        Code.Code.Make(Opcode.OpConstant, 5),
                        Code.Code.Make(Opcode.OpMul),
                        Code.Code.Make(Opcode.OpHash, 4),
                        Code.Code.Make(Opcode.OpPop)
                    ),
                    new object[]{
                        new IntegerObject(1),
                        new IntegerObject(2),
                        new IntegerObject(3),
                        new IntegerObject(4),
                        new IntegerObject(5),
                        new IntegerObject(6)
                    }
                },
            };


    [Fact]
    public void Symbol_defines_symbols()
    {
        var global = new SymbolTable();
        var a = global.Define("a");
        Assert.Equal(new Symbol("a", Symbol.GLOBAL_SCOPE, 0), a);

        var b = global.Define("b");
        Assert.Equal(new Symbol("b", Symbol.GLOBAL_SCOPE, 1), b);
    }

    [Fact]
    public void Symbol_resolves_symbols()
    {
        var global = new SymbolTable();
        global.Define("a");
        global.Define("b");

        var a = global.Resolve("a");
        Assert.Equal(new Symbol("a", Symbol.GLOBAL_SCOPE, 0), a);

        var b = global.Resolve("b");
        Assert.Equal(new Symbol("b", Symbol.GLOBAL_SCOPE, 1), b);
    }

    private static string Disassemble(params Maybe<byte[]>[] operations)
        => Code.Code.Disassemble(operations.SelectMany(x => x.Value).ToArray()).Value;

    private Helpers.Maybe<AstProgram> Parse(string Input)
    {
        var lexer = Lexer.ParseTokens(Input);
        var parser = new Parser(lexer, new XUnitLogger<Parser>(outputHelper));
        return parser.ParseProgram();
    }
}
