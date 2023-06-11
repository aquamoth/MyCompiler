using MyCompiler.Helpers;
using System.Text;

namespace MyCompiler.Code;

public static class Code
{
    //public readonly byte[] Instructions;
    //public byte Opcode;
    static readonly IDictionary<Opcode, Definition> Definitions;

    static Code()
    {
        Code.Definitions = new[] {
            new Definition { Opcode = MyCompiler.Code.Opcode.OpConstant, Name = "OpConstant", OperandWidths = new[] { 2 } }
        }.ToDictionary(x => x.Opcode);
    }

    public static Maybe<Definition> Lookup(byte opcode)
    {
        if (!Code.Definitions.TryGetValue((Opcode)opcode, out var d))
            return new Exception("Opcode not found in definition");

        return d;
    }

    public static Maybe<byte[]> Make(Opcode opcode, params int[] operands)
    {
        var definition = Definitions[opcode];

        var instructionLen = 1 + definition.OperandWidths.Sum();

        var instruction = new byte[instructionLen];
        instruction[0] = (byte)opcode;

        var offset = 1;
        for (var i = 0; i < operands.Length; i++)
        {
            var operand = operands[i];
            var width = definition.OperandWidths[i];
            switch (width)
            {
                case 2:
                    instruction[offset] = (byte)(operand >> 8);
                    instruction[offset + 1] = (byte)operand;
                    break;
            }
            offset += width;
        }

        return instruction;
    }

    public static Maybe<string> Disassemble(Span<byte> Instructions)
    {
        StringBuilder disassemble = new();

        var offset = 0;
        while (offset < Instructions.Length)
        {
            var instruction = Instructions[offset];
            var definition = Lookup(instruction);
            if (definition.HasError)
                return definition.Error!;

            disassemble.Append($"{offset:0000}");
            disassemble.Append(' ');
            disassemble.Append(definition.Value.Name);
            offset++;

            for (var i = 0; i < definition.Value.OperandWidths.Length; i++)
            {
                var width = definition.Value.OperandWidths[i];
                
                var operand = ReadOperand(Instructions[offset..(offset+width)]);
                if (operand.HasError)
                    return operand.Error!;

                offset += width;
                disassemble.Append(' ');
                disassemble.Append(operand.Value);
            }

            disassemble.AppendLine();
        }

        disassemble.Length -= Environment.NewLine.Length;
        return disassemble.ToString();
    }

    private static Maybe<object> ReadOperand(Span<byte> span)
    {
        return span.Length switch
        {
            2 => (ushort)((span[0] << 8) | span[1]),
            _ => new Exception("Unhandled operand width"),
        };
    }
}
