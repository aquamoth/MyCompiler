using MyCompiler.Helpers;

namespace MyCompiler.Entities;

public class EnvironmentStore
{
    private readonly Dictionary<string, IObject> _store = new();

    private EnvironmentStore()
    {
    }
        
    public Result<IObject> Get(string name)
    {
        if (_store.TryGetValue(name, out var value))
        {
            return Result<IObject>.Success(value);
        }

        return new Exception($"identifier not found: {name}");
    }

    public void Set(string name, IObject value)
    {
        _store[name] = value;
    }

    public static EnvironmentStore New()
    {
        return new EnvironmentStore();
    }
}