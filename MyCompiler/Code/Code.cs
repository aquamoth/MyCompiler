using MyCompiler.Helpers;
using System.Buffers.Binary;
using System.Text;

namespace MyCompiler.Code;

public static class Code
{
    static readonly IDictionary<Opcode, Definition> definitions;

    static Code()
    {
        definitions = new[] {
            Define(Opcode.OpConstant, 2),
            Define(Opcode.OpArray, 2),
            
            Define(Opcode.OpAdd),
            Define(Opcode.OpSub),
            Define(Opcode.OpMul),
            Define(Opcode.OpDiv),
            
            Define(Opcode.OpPop),
            
            Define(Opcode.OpTrue),
            Define(Opcode.OpFalse),
            Define(Opcode.OpNull),
            
            Define(Opcode.OpEqual),
            Define(Opcode.OpNotEqual),
            Define(Opcode.OpGreaterThan),
            Define(Opcode.OpMinus),
            Define(Opcode.OpBang),

            Define(Opcode.OpJumpNotTruthy, 2),
            Define(Opcode.OpJump, 2),

            Define(Opcode.OpGetGlobal, 2),
            Define(Opcode.OpSetGlobal, 2),
        }.ToDictionary(x => x.Opcode);
    }

    public static Maybe<Definition> Lookup(byte opcode)
    {
        if (!definitions.TryGetValue((Opcode)opcode, out var d))
            return new Exception("Opcode not found in definition");

        return d;
    }

    public static Maybe<byte[]> Make(Opcode opcode, params int[] operands)
    {
        var definition = definitions[opcode];

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
                    BinaryPrimitives.WriteUInt16BigEndian(instruction.AsSpan()[1..], (ushort)operand);
                    break;
                default:
                    return new Exception("Unhandled operand width");
            }
            offset += width;
        }

        return instruction;
    }

    public static Maybe<string> Disassemble(Span<byte> bytecode)
    {
        var instructions = DisassembleIt(bytecode);
        if (instructions.HasError)
            return instructions.Error!;

        StringBuilder disassemble = new();
        var offset = 0;
        foreach(var (instruction, operands) in instructions.Value)
        {
            disassemble.Append($"{offset:0000}");
            disassemble.Append(' ');
            disassemble.Append(instruction);
            offset++;

            for (var i = 0; i < operands.Length; i++)
            {
                offset += operands[i].width;
                disassemble.Append(' ');
                disassemble.Append(operands[i].value);
            }

            disassemble.AppendLine();
        }

        disassemble.Length -= Environment.NewLine.Length;
        return disassemble.ToString();
    }

    public static Maybe<(string instruction, (object value, int width)[] operands)[]> DisassembleIt(Span<byte> Instructions)
    {
        var items = new List<(string name, (object value, int width)[] args)>();

        var offset = 0;
        while (offset < Instructions.Length)
        {
            var instruction = Instructions[offset];
            var definition = Lookup(instruction);
            if (definition.HasError)
                return definition.Error!;
            offset++;

            int[] operandWidths = definition.Value.OperandWidths;
            var operands = ReadOperands(Instructions, offset, operandWidths);
            if (operands.HasError)
                return operands.Error!;

            offset += operandWidths.Sum();

            var operandsWithWidths = operands.Value.Zip(operandWidths,
                (value, width) => (value, width)
            ).ToArray();

            items.Add(
                (definition.Value.Name, operandsWithWidths)
            );
        }

        return items.ToArray();
    }

    public static Maybe<object[]> ReadOperands(Span<byte> Instructions, int offset, int[] operandWidths)
    {
        var operands = new List<object>(operandWidths.Length);
        for (var i = 0; i < operandWidths.Length; i++)
        {
            var width = operandWidths[i];

            var operand = ReadOperand(Instructions[offset..], width);
            if (operand.HasError)
                return operand.Error!;

            offset += width;
            operands.Add(operand.Value);
        }

        return operands.ToArray();
    }

    public static Maybe<object> ReadOperand(Span<byte> span, int length)
    {
        return length switch
        {
            2 => BinaryPrimitives.ReadUInt16BigEndian(span),
            _ => new Exception("Unhandled operand width"),
        };
    }

    private static Definition Define(Opcode opcode, params int[] operandWidths) 
        => new Definition(opcode, opcode.ToString(), operandWidths);
}
