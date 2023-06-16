using MyCompiler.Code;
using MyCompiler.Entities;

namespace MyCompiler.Tests
{
    public class Code_
    {
        [Theory]
        [MemberData(nameof(Makes_Opcodes_into_Bytecodes_VALUES))]
        public void Makes_Opcodes_into_Bytecodes(Opcode opcode, int[] operands, byte[] bytecode)
        {
            var instruction = Code.Code.Make(opcode, operands);
            Assert.True(instruction.HasValue, instruction.Error?.Message);

            Assert.Equal(bytecode, instruction.Value);
        }
        public static TheoryData<Opcode, int[], byte[]> Makes_Opcodes_into_Bytecodes_VALUES
        {
            get
            {
                return new()
                {
                    {Opcode.OpConstant, new[]{ 65534 }, new byte[] { (byte)Opcode.OpConstant, 0xFF, 0xFE } },
                    {Opcode.OpAdd, Array.Empty<int>(), new byte[]{ (byte)Opcode.OpAdd } },
                    {Opcode.OpSub, Array.Empty<int>(), new byte[]{ (byte)Opcode.OpSub } },
                    {Opcode.OpMul, Array.Empty<int>(), new byte[]{ (byte)Opcode.OpMul } },
                    {Opcode.OpDiv, Array.Empty<int>(), new byte[]{ (byte)Opcode.OpDiv } },
                    {Opcode.OpPop, Array.Empty<int>(), new byte[]{ (byte)Opcode.OpPop } },
                    {Opcode.OpGetLocal, new int[]{ 255 }, new byte[]{ (byte)Opcode.OpGetLocal, 0xFF } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(Makes_and_disassembles_code_VALUES))]
        public void Makes_and_disassembles_code(byte[] instructions, string expected)
        {
            var disassembled = Code.Code.Disassemble(instructions);
            Assert.True(disassembled.HasValue, disassembled.Error?.Message);
            Assert.Equal(expected, disassembled.Value);
        }
        public static TheoryData<byte[], string> Makes_and_disassembles_code_VALUES => new TheoryData<byte[], string>()
            {
                {
                    new[]
                    {
                        Code.Code.Make(Opcode.OpConstant, 1).Value,
                        Code.Code.Make(Opcode.OpConstant, 2).Value,
                        Code.Code.Make(Opcode.OpConstant, 65535).Value
                    }.SelectMany(x=>x).ToArray(),
                    """
                    0000 OpConstant 1
                    0003 OpConstant 2
                    0006 OpConstant 65535
                    """
                },
                {
                    new[]
                    {
                        Code.Code.Make(Opcode.OpAdd).Value,
                        Code.Code.Make(Opcode.OpConstant, 2).Value,
                        Code.Code.Make(Opcode.OpConstant, 65535).Value
                    }.SelectMany(x=>x).ToArray(),
                    """
                    0000 OpAdd
                    0001 OpConstant 2
                    0004 OpConstant 65535
                    """
                },
                {
                    new[]
                    {
                        Code.Code.Make(Opcode.OpAdd).Value,
                        Code.Code.Make(Opcode.OpGetLocal, 1).Value,
                        Code.Code.Make(Opcode.OpConstant, 2).Value,
                        Code.Code.Make(Opcode.OpConstant, 65535).Value,
                    }.SelectMany(x=>x).ToArray(),
                    """
                    0000 OpAdd
                    0001 OpGetLocal 1
                    0003 OpConstant 2
                    0006 OpConstant 65535
                    """
                }
            };
    }
}