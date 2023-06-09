namespace MyCompiler.Helpers;

public readonly struct Maybe<T>
{
    private readonly T? value;
    private readonly Exception? error;

    public bool HasValue { get; init; }
    public bool HasError => !HasValue;

    public T Value => HasValue ? value! : throw new InvalidOperationException();
    public Exception? Error => HasError ? error : null;

    private Maybe(T value)
    {
        this.HasValue = true;
        this.value = value;
        this.error = default;
    }

    private Maybe(Exception error)
    {
        this.HasValue = false;
        this.value = default;
        this.error = error;
    }

    public static Maybe<T> From(T value) => new(value);
    //public static Maybe<T> Failure(Exception ex) => new(ex);

    public static implicit operator Maybe<T>(T value) => new(value);

    //public static explicit operator T(Maybe<T> result) => result.Value;

    public static implicit operator Maybe<T>(Exception ex) => new(ex);
}
