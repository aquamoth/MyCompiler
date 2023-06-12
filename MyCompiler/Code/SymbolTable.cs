using MyCompiler.Helpers;

namespace MyCompiler.Code;

public class SymbolTable
{
    IDictionary<string, Symbol> store = new Dictionary<string, Symbol>();
    int numDefinitions = 0;

    public Maybe<Symbol> Define(string name)
    {
        var symbol = new Symbol(name, Symbol.GLOBAL_SCOPE, numDefinitions);
        if (!store.TryAdd(name, symbol))
            return new Exception($"symbol {name} already defined");

        numDefinitions++;
        return symbol;
    }

    public Maybe<Symbol> Resolve(string name)
    {
        if (!store.TryGetValue(name, out var symbol))
            return new Exception($"unknown symbol {name}");

        return symbol;
    }
}
