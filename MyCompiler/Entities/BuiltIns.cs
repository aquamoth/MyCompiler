using MyCompiler.Helpers;

namespace MyCompiler.Entities;

public static class BuiltIns
{
    static readonly Dictionary<string, BuiltIn> dictionary = new();

    static BuiltIns()
    {
        dictionary.Add("len", new BuiltIn(BUILTIN_LEN));
        dictionary.Add("first", new BuiltIn(BUILTIN_FIRST));
        dictionary.Add("last", new BuiltIn(BUILTIN_LAST));
        dictionary.Add("rest", new BuiltIn(BUILTIN_REST));
        dictionary.Add("push", new BuiltIn(BUILTIN_PUSH));
        dictionary.Add("gets", new BuiltIn(BUILTIN_GETS));
        dictionary.Add("puts", new BuiltIn(BUILTIN_PUTS));
    }

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

            _ => new Exception($"Expected {ObjectType.ARRAY} but got {args[0].Type}")
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

            _ => new Exception($"Expected {ObjectType.ARRAY} but got {args[0].Type}")
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

            _ => new Exception($"Expected {ObjectType.ARRAY} but got {args[0].Type}")
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

            _ => new Exception($"Expected {ObjectType.ARRAY} but got {args[0].Type}")
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