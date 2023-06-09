namespace MyCompiler.Helpers;

//public readonly struct Result
//{
//    public bool IsSuccess { get; init; }
//    public Exception? Error { get; init; }

//    public static Result Failure(Exception ex) => new(ex);
    
//    public Result()
//    {
//        this.IsSuccess = true;
//        this.Error = null;
//    }

//    private Result(Exception error)
//    {
//        this.IsSuccess = false;
//        this.Error = error;
//    }

//    public static Result<T> Call<T>(Func<T> func)
//    {
//        try
//        {
//            return Result<T>.Success(func());
//        }
//        catch (Exception ex)
//        {
//            return Result<T>.Failure(ex);
//        }
//    }

//    public static Result Call(Action action)
//    {
//        try
//        {
//            action();
//            return new Result();
//        }
//        catch (Exception ex)
//        {
//            return Failure(ex);
//        }
//    }
//}

public readonly struct Maybe<T>
{
    private readonly T? value;
    private readonly Exception? error;

    public bool HasValue { get; init; }
    public bool HasError => !HasValue;

    public T Value => HasValue ? value! : throw new InvalidOperationException();
    public Exception? Error => HasError ? error! : null;

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

    public static Maybe<T> Success(T value) => new(value);
    public static Maybe<T> Failure(Exception ex) => new(ex);

    public static implicit operator Maybe<T>(T value) => new(value);
    public static explicit operator T(Maybe<T> result) => result.Value;

    public static implicit operator Maybe<T>(Exception ex) => new(ex);
}
