using MyCompiler.Helpers;
using System.Diagnostics;

namespace MyCompiler.Entities;

public enum ObjectType
{
    INTEGER,
    STRING,
    BOOLEAN,
    NULL,
    RETURN_VALUE,
    FUNCTION,
    BUILTIN
}

public interface IObject
{
    ObjectType Type { get; init; }
    string Inspect();
}

[DebuggerDisplay("{Value,nq}")]
public class IntegerObject : IObject
{
    public ObjectType Type { get; init; } = ObjectType.INTEGER;
    public long Value { get; init; }
    public string Inspect() => Value.ToString();
}

[DebuggerDisplay("{Value,nq}")]
public class StringObject : IObject
{
    public ObjectType Type { get; init; } = ObjectType.STRING;
    public string Value { get; init; }
    public string Inspect() => $"{Value}";
}


[DebuggerDisplay("{Value,nq}")]
public class BooleanObject : IObject
{
    private BooleanObject(bool value)
    {
        Value = value;
    }

    public ObjectType Type { get; init; } = ObjectType.BOOLEAN;
    public bool Value { get; init; }
    public string Inspect() => Value.ToString();

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
    public Func<IObject[], Result<IObject>> Fn { get; init; }

    //public Identifier[] Parameters { get; init; }
    //public BlockStatement Body { get; init; }
    //public EnvironmentStore Env { get; init; }

    public BuiltIn(Func<IObject[], Result<IObject>> fn)
    {
        Fn = fn;
    }

    public string Inspect() => $"builtin function";
}

