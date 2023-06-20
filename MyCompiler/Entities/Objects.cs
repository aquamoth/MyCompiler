using MyCompiler.Helpers;
using System.Diagnostics;
using System.Text;

namespace MyCompiler.Entities;

public enum ObjectType
{
    UNDEFINED,
    INTEGER,
    STRING,
    BOOLEAN,
    NULL,
    RETURN_VALUE,
    FUNCTION,
    COMPILED_FUNCTION,
    CLOSURE,
    BUILTIN,
    ARRAY,
    HASH
}

public interface IObject
{
    ObjectType Type { get; init; }
    string Inspect();
}

public interface IHashable : IObject
{
    HashKey HashKey();
}

[DebuggerDisplay("{Value,nq}")]
public record IntegerObject(long Value) : IObject, IHashable
{
    public ObjectType Type { get; init; } = ObjectType.INTEGER;

    public string Inspect() => Value.ToString();
    public HashKey HashKey() => new(this.Type, this.Value);
}

[DebuggerDisplay("{Value,nq}")]
public record StringObject(string Value) : IObject, IHashable
{
    public ObjectType Type { get; init; } = ObjectType.STRING;

    public string Inspect() => $"\"{Value}\"";

    public HashKey HashKey()
    {
        long hashCode = 0L;
        long pow = 1L;
        foreach (var b in Encoding.UTF8.GetBytes(this.Value))
        {
            hashCode = (hashCode + b * pow) % 1000000009;
            pow = (pow * 31) % 1000000009;
        }

        return new HashKey(Type, hashCode);
    }
}

[DebuggerDisplay("[{Elements,nq}]")]
public sealed record ArrayObject(IObject[] Elements) : IObject
{
    public ObjectType Type { get; init; } = ObjectType.ARRAY;

    public string Inspect() => $"[{string.Join(",", Elements.Select(e => e.Inspect()))}]";

    public bool Equals(ArrayObject? obj) => obj is not null && Elements.SequenceEqual(obj.Elements);

    public override int GetHashCode() => HashCode.Combine(Elements);

    public override string ToString() => Inspect();
}

[DebuggerDisplay("{Value,nq}")]
public class BooleanObject : IObject, IHashable
{
    private BooleanObject(bool value)
    {
        Value = value;
    }

    public ObjectType Type { get; init; } = ObjectType.BOOLEAN;
    public bool Value { get; init; }
    public string Inspect() => Value.ToString();
    public HashKey HashKey() => new(this.Type, this.Value ? 1 : 0);

    public readonly static BooleanObject True = new(true);
    public readonly static BooleanObject False = new(false);
}

[DebuggerDisplay("null")]
public class NullObject : IObject
{
    private NullObject()
    {
    }

    public ObjectType Type { get; init; } = ObjectType.NULL;
    public string Inspect() => "";

    public readonly static NullObject Value = new();
}

[DebuggerDisplay("return {Value,nq}")]
public record ReturnValue(IObject Value) : IObject
{
    public ObjectType Type { get; init; } = ObjectType.RETURN_VALUE;

    public string Inspect() => Value.ToString();
}

//[DebuggerDisplay("{Value,nq}")]
public record FunctionObject(Identifier[] Parameters, BlockStatement Body, EnvironmentStore Env) : IObject
{
    public ObjectType Type { get; init; } = ObjectType.FUNCTION;

    public string Inspect() => $"fn({string.Join(", ", Parameters)}) {{\n{Body}\n}}";
}

//[DebuggerDisplay("{Value,nq}")]
public record BuiltIn(Func<IObject[], Maybe<IObject>> Fn) : IObject
{
    public ObjectType Type { get; init; } = ObjectType.BUILTIN;

    public string Inspect() => $"builtin function";
}

//[DebuggerDisplay("{Value,nq}")]
public class HashObject : IObject, IEquatable<HashObject>
{
    public HashObject()
    {

    }

    public HashObject(params (IHashable key, IObject value)[] values)
    {
        foreach (var value in values)
        {
            Pairs.Add(value.key.HashKey(), new HashPair(value.key, value.value));
        }
    }
    public ObjectType Type { get; init; } = ObjectType.HASH;
    public IDictionary<HashKey, HashPair> Pairs { get; init; } = new Dictionary<HashKey, HashPair>();

    public bool Equals(HashObject? other)
    {
        if (other is null) return false;
        return Pairs.Count == other.Pairs.Count
            && Pairs.All(p => other.Pairs.TryGetValue(p.Key, out var otherValue)
                                && p.Value.Equals(otherValue)
                        );
    }

    public string Inspect() => $"{{ {string.Join(", ", Pairs.Values.Select(p => $"{p.Key.Inspect()}: {p.Value.Inspect()}"))} }}";

    public override string ToString() => Inspect();
}

public readonly record struct HashPair(IObject Key, IObject Value);

public readonly record struct HashKey(ObjectType Type, long HashCode);


record CompiledFunction(byte[] Instructions, int NumberOfLocals, int NumberOfParameters) : IObject, IEquatable<CompiledFunction>
{
    public ObjectType Type { get; init; } = ObjectType.COMPILED_FUNCTION;
    public string Inspect() => $@"CompiledFunction(
{Code.Code.Disassemble(Instructions).Value}
)";

    public override string ToString() => Inspect();

    public virtual bool Equals(CompiledFunction? other)
    {
        if (other is null) return false;
        return Instructions.SequenceEqual(other.Instructions);
    }
}

record Closure(CompiledFunction Function, params IObject[] Free) : IObject //, IEquatable<Closure>
{
    public ObjectType Type { get; init; } = ObjectType.CLOSURE;
    public string Inspect() => $"Closure({Function.Inspect()})";
    public override string ToString() => Inspect();
    //public bool Equals(Closure? other)
    //{
    //    if (other is null) return false;
    //    return function.Equals(other.function) && Free.SequenceEqual(other.Free);
    //}
}