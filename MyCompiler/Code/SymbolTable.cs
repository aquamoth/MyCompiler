using MyCompiler.Helpers;
using static System.Formats.Asn1.AsnWriter;

namespace MyCompiler.Code;

public class SymbolTable
{
    public readonly SymbolTable? Outer;
    internal readonly IDictionary<string, Symbol> store = new Dictionary<string, Symbol>();
    private int numDefinitions = 0;

    public SymbolTable() : this(null)
    {
    }

    public SymbolTable(SymbolTable? outer)
    {
        this.Outer = outer;
        this.store = new Dictionary<string, Symbol>();
        this.numDefinitions = 0;
    }

    public Maybe<Symbol> Define(string name)
    {
        var scope = Outer == null ? Symbol.GLOBAL_SCOPE : Symbol.LOCAL_SCOPE;

        var symbol = new Symbol(name, scope, numDefinitions);
        if (!store.TryAdd(name, symbol))
            return new Exception($"symbol {name} already defined");

        numDefinitions++;
        return symbol;
    }

    internal Maybe<Symbol> DefineBuiltin(int index, string name)
    {
        var symbol = new Symbol(name, Symbol.BUILTIN_SCOPE, index);
        if (!store.TryAdd(name, symbol))
            return new Exception($"symbol {name} already defined");

        return symbol;
    }

    public Maybe<Symbol> Resolve(string name)
    {
        if (store.TryGetValue(name, out var symbol))
            return symbol;

        if (Outer != null)
            return Outer.Resolve(name);

        return new Exception($"unknown symbol {name}");
    }
}
