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

public readonly struct Result<T>
{
    private readonly T? value;
    private readonly Exception? error;

    public bool IsSuccess { get; init; }
    public T Value => IsSuccess ? value! : throw new InvalidOperationException();
    public Exception? Error => !IsSuccess ? error! : null;

    private Result(T value)
    {
        this.IsSuccess = true;
        this.value = value;
        this.error = default;
    }

    private Result(Exception error)
    {
        this.IsSuccess = false;
        this.value = default;
        this.error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Exception ex) => new(ex);

    public static implicit operator Result<T>(T value) => new(value);
    public static explicit operator T(Result<T> result) => result.Value;

    public static implicit operator Result<T>(Exception ex) => new(ex);
}
