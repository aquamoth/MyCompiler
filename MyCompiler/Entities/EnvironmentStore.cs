using MyCompiler.Helpers;

namespace MyCompiler.Entities;

public class EnvironmentStore
{
    private readonly Dictionary<string, IObject> _store = new();
    private readonly EnvironmentStore? _outer = null;

    private EnvironmentStore(EnvironmentStore? outer = null)
    {
        _outer = outer;
    }
        
    public Maybe<IObject> Get(string name)
    {
        if (_store.TryGetValue(name, out var value))
        {
            return Maybe<IObject>.Success(value);
        }

        if (_outer != null)
        {
            return _outer.Get(name);
        }

        return new Exception($"identifier not found: {name}");
    }

    public void Set(string name, IObject value)
    {
        _store[name] = value;
    }

    public static EnvironmentStore New() => new();

    public static EnvironmentStore NewEnclosed(EnvironmentStore outer) => new(outer);
}