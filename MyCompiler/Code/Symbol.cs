namespace MyCompiler.Code;

public record struct Symbol(string Name, string Scope, int Index)
{
    public const string GLOBAL_SCOPE = "GLOBAL";
    public const string LOCAL_SCOPE = "LOCAL";
}
