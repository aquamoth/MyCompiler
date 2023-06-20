using MyCompiler.Helpers;

namespace MyCompiler.Code;

public class SymbolTable
{
    private int numDefinitions = 0;

    internal IDictionary<string, Symbol> Store { get; init; }
    internal SymbolTable? Outer { get; init; }
    internal List<Symbol> FreeSymbols { get; } = new();

    public SymbolTable() : this(null)
    {
    }

    public SymbolTable(SymbolTable? outer)
    {
        Outer = outer;
        Store = new Dictionary<string, Symbol>();
        numDefinitions = 0;
    }

    public Maybe<Symbol> Define(string name)
    {
        var scope = Outer == null ? Symbol.GLOBAL_SCOPE : Symbol.LOCAL_SCOPE;

        var symbol = new Symbol(name, scope, numDefinitions);
        if (!Store.TryAdd(name, symbol))
            return new Exception($"symbol {name} already defined");

        numDefinitions++;
        return symbol;
    }

    public Maybe<Symbol> DefineFree(Symbol original)
    {
        FreeSymbols.Add(original);
        var symbol = new Symbol(original.Name, Symbol.FREE_SCOPE, FreeSymbols.Count - 1);

        if (!Store.TryAdd(symbol.Name, symbol))
            return new Exception($"symbol {symbol.Name} already defined");

        return symbol;
    }

    internal Maybe<Symbol> DefineBuiltin(int index, string name)
    {
        var symbol = new Symbol(name, Symbol.BUILTIN_SCOPE, index);
        if (!Store.TryAdd(name, symbol))
            return new Exception($"symbol {name} already defined");

        return symbol;
    }

    public Maybe<Symbol> Resolve(string name)
    {
        if (Store.TryGetValue(name, out var symbol))
            return symbol;

        if (Outer == null)
            return new Exception($"unknown symbol {name}");

        var outerSymbol = Outer.Resolve(name);
        if (outerSymbol.HasError)
            return outerSymbol;

        symbol = outerSymbol.Value;
        if (symbol.Scope == Symbol.GLOBAL_SCOPE || symbol.Scope == Symbol.BUILTIN_SCOPE)
            return outerSymbol;

        var freeSymbol = DefineFree(symbol);
        return freeSymbol;
    }
}
