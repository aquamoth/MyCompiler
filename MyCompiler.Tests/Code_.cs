using MyCompiler.Code;
using System.Linq;
using Xunit.Abstractions;

namespace MyCompiler.Tests
{
    public class Code_
    {
        [Theory]
        [InlineData(Opcode.OpConstant, 65534)]
        public void Compiles_constants_to_IL(Opcode opcode, int operand)
        {
            var instruction = Code.Code.Make(opcode, operand);
            Assert.True(instruction.HasValue);

            Assert.Equal(new byte[] { 0x00, 0xFF, 0xFE }, instruction.Value);
        }

        [Fact]
        public void Disassembles_IL_code()
        {
            var instructions = Code.Code.Make(Opcode.OpConstant, 1).Value
                .Concat(Code.Code.Make(Opcode.OpConstant, 2).Value)
                .Concat(Code.Code.Make(Opcode.OpConstant, 65535).Value)
                .ToArray();
         
            var disassembled = Code.Code.Disassemble(instructions);
            Assert.True(disassembled.HasValue);

            var expected = """
                0000 OpConstant 1
                0003 OpConstant 2
                0006 OpConstant 65535
                """;

            Assert.Equal(expected, disassembled.Value);
        }
    }
}