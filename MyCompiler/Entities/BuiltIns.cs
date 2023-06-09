﻿using MyCompiler.Helpers;

namespace MyCompiler.Entities;

public static class BuiltIns
{
    public static readonly (string Name, BuiltIn Fn)[] Functions = new (string Name, BuiltIn Fn)[]
    {
        ("len"  , new BuiltIn(BUILTIN_LEN)  ),
        ("puts" , new BuiltIn(BUILTIN_PUTS) ),
        ("first", new BuiltIn(BUILTIN_FIRST)),
        ("last" , new BuiltIn(BUILTIN_LAST) ),
        ("rest" , new BuiltIn(BUILTIN_REST) ),
        ("push" , new BuiltIn(BUILTIN_PUSH) ),
        ("gets" , new BuiltIn(BUILTIN_GETS) ),
    };

    static readonly Dictionary<string, BuiltIn> dictionary = new()
    {
        { "len"  , new BuiltIn(BUILTIN_LEN  ) },
        { "puts" , new BuiltIn(BUILTIN_PUTS ) },
        { "first", new BuiltIn(BUILTIN_FIRST) },
        { "last" , new BuiltIn(BUILTIN_LAST ) },
        { "rest" , new BuiltIn(BUILTIN_REST ) },
        { "push" , new BuiltIn(BUILTIN_PUSH ) },
        { "gets" , new BuiltIn(BUILTIN_GETS ) },
    };

    public static Maybe<BuiltIn> GetByName(string name)
    {
        if (dictionary.TryGetValue(name, out var builtin))
            return builtin;

        return new Exception($"Builtin with name {name} not found");
    }


    private static Maybe<IObject> BUILTIN_LEN(IObject[] args)
    {
        if (args.Length != 1)
            return new Exception($"wrong number of arguments. got={args.Length}, want=1");

        return args[0] switch
        {
            ArrayObject array => new IntegerObject(array.Elements.Length),
            StringObject str => new IntegerObject(str.Value.Length),
            _ => new Exception($"argument to `len` not supported, got {args[0].Type}")
        };
    }

    private static Maybe<IObject> BUILTIN_FIRST(IObject[] args)
    {
        if (args.Length != 1)
            return new Exception($"wrong number of arguments. got={args.Length}, want=1");

        return args[0] switch
        {
            ArrayObject arg0 => Maybe<IObject>.From(
                arg0.Elements.Length == 0 ? NullObject.Value : arg0.Elements[0]
            ),

            _ => new Exception($"argument to `first` must be ARRAY, got {args[0].Type}")
        };
    }

    private static Maybe<IObject> BUILTIN_LAST(IObject[] args)
    {
        if (args.Length != 1)
            return new Exception($"wrong number of arguments. got={args.Length}, want=1");

        return args[0] switch
        {
            ArrayObject arg0 => Maybe<IObject>.From(
                arg0.Elements.Length == 0 ? NullObject.Value : arg0.Elements[^1]
            ),

            _ => new Exception($"argument to `last` must be ARRAY, got {args[0].Type}")
        };
    }

    private static Maybe<IObject> BUILTIN_REST(IObject[] args)
    {
        if (args.Length != 1)
            return new Exception($"wrong number of arguments. got={args.Length}, want=1");

        return args[0] switch
        {
            ArrayObject arg0 => Maybe<IObject>.From(
                arg0.Elements.Length == 0 ? NullObject.Value : new ArrayObject(arg0.Elements[1..])
            ),

            _ => new Exception($"argument to `rest` must be ARRAY, got {args[0].Type}")
        };
    }

    private static Maybe<IObject> BUILTIN_PUSH(IObject[] args)
    {
        if (args.Length != 2)
            return new Exception($"wrong number of arguments. got={args.Length}, want=2");

        return args[0] switch
        {
            ArrayObject arg0 => Maybe<IObject>.From(
                new ArrayObject(arg0.Elements.Concat(new[] { args[1] }).ToArray())
            ),

            _ => new Exception($"argument to `push` must be ARRAY, got {args[0].Type}")
        };
    }

    private static Maybe<IObject> BUILTIN_GETS(IObject[] args)
    {
        if (args.Length != 0)
            return new Exception($"wrong number of arguments. got={args.Length}, want=0");

        var s = Console.ReadLine();
        if (s == null)
            return new Exception("Received no string from console input");

        return new StringObject(s);
    }

    private static Maybe<IObject> BUILTIN_PUTS(IObject[] args)
    {
        foreach (var arg in args)
        {
            Console.WriteLine(arg.Inspect());
        }

        return NullObject.Value;
    }
}