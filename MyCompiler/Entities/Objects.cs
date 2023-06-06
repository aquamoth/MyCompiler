﻿using System.Diagnostics;

namespace MyCompiler.Entities;

public enum ObjectType
{
    Integer,
    Boolean,
    Null,
}

public interface IObject
{
    ObjectType Type { get; init; }
    string Inspect();
}

[DebuggerDisplay("{Value,nq}")]
public class IntegerObject : IObject
{
    public ObjectType Type { get; init; } = ObjectType.Integer;
    public long Value { get; init; }
    public string Inspect() => Value.ToString();
}

[DebuggerDisplay("{Value,nq}")]
public class BooleanObject : IObject
{
    private BooleanObject(bool value)
    {
        Value = value;
    }

    public ObjectType Type { get; init; } = ObjectType.Boolean;
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

    public ObjectType Type { get; init; } = ObjectType.Null;
    public string Inspect() => "null";

    public readonly static NullObject Value = new();
}