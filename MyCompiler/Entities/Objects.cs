using MyCompiler.Helpers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MyCompiler.Entities;

public enum ObjectType
{
    INTEGER,
    STRING,
    BOOLEAN,
    NULL,
    RETURN_VALUE,
    FUNCTION,
    BUILTIN,
    ARRAY,
    HASH
}

public interface IObject
{
    ObjectType Type { get; init; }
    string Inspect();
}

public interface IHashable
{
    HashKey HashKey();
}

[DebuggerDisplay("{Value,nq}")]
public class IntegerObject : IObject, IHashable, IEquatable<IntegerObject>
{
    public ObjectType Type { get; init; } = ObjectType.INTEGER;
    public long Value { get; init; }
    public string Inspect() => Value.ToString();
    public HashKey HashKey() => new(this.Type, this.Value);

    public bool Equals(IntegerObject? other)
    {
        return other != null && other.Value == this.Value;
    }

    public IntegerObject()
    {
    }

    public IntegerObject(long value)
    {
        Value = value;
    }
}

[DebuggerDisplay("{Value,nq}")]
public class StringObject : IObject, IHashable
{
    public ObjectType Type { get; init; } = ObjectType.STRING;
    public string Value { get; init; }
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
public class ArrayObject : IObject
{
    public ObjectType Type { get; init; } = ObjectType.ARRAY;
    public IObject[] Elements { get; init; }
    public string Inspect() => $"[{string.Join(",", Elements.Select(e => e.Inspect()))}]";

    public ArrayObject(IObject[] elements)
    {
        Elements = elements;
    }
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
public class ReturnValue : IObject
{
    public ReturnValue(IObject value)
    {
        Value = value;
    }

    public ObjectType Type { get; init; } = ObjectType.RETURN_VALUE;
    public IObject Value { get; init; }
    public string Inspect() => Value.ToString();
}

//[DebuggerDisplay("{Value,nq}")]
public class FunctionObject : IObject
{
    public ObjectType Type { get; init; } = ObjectType.FUNCTION;
    public Identifier[] Parameters { get; init; }
    public BlockStatement Body { get; init; }
    public EnvironmentStore Env { get; init; }

    public FunctionObject(Identifier[] parameters, BlockStatement body, EnvironmentStore env)
    {
        Parameters = parameters;
        Body = body;
        Env = env;
    }

    public string Inspect() => $"fn({string.Join(", ", Parameters)}) {{\n{Body}\n}}";
}

//[DebuggerDisplay("{Value,nq}")]
public class BuiltIn : IObject
{
    public ObjectType Type { get; init; } = ObjectType.BUILTIN;
    public Func<IObject[], Maybe<IObject>> Fn { get; init; }

    public BuiltIn(Func<IObject[], Maybe<IObject>> fn)
    {
        Fn = fn;
    }

    public string Inspect() => $"builtin function";
}

//[DebuggerDisplay("{Value,nq}")]
public class HashObject : IObject
{
    public ObjectType Type { get; init; } = ObjectType.HASH;
    public IDictionary<HashKey, HashPair> Pairs { get; init; } = new Dictionary<HashKey, HashPair>();

    public string Inspect() => $"{{ {string.Join(", ", Pairs.Values.Select(p => $"{p.Key.Inspect()}: {p.Value.Inspect()}"))} }}";
}

public readonly struct HashPair
{
    public IObject Key { get; init; }
    public IObject Value { get; init; }

    public HashPair(IObject key, IObject value)
    {
        Key = key;
        Value = value;
    }
}




public readonly struct HashKey : IEquatable<HashKey>
{
    private readonly ObjectType type;
    private readonly long hashCode;

    public HashKey(ObjectType type, long hashCode)
    {
        this.type = type;
        this.hashCode = hashCode;
    }

    public bool Equals(HashKey other)
    {
        return this.type == other.type && this.hashCode == other.hashCode;
    }
}
