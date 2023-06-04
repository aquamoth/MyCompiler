using MyCompiler.Helpers;

namespace MyCompiler.Tests;

public class Result_
{
    public void Runner()
    {
        var result = MyFunctionThatResults(69);
        Console.WriteLine((string)result);

        if (result.IsSuccess)
        {

        }
    }

    public Result<string> MyFunctionThatResults(int i)
    {
        return Result.Call(() => MyUnsafeFunctionThatThrows(i));
    }


    public string MyUnsafeFunctionThatThrows(int i)
    {
        if (i == 69)
            throw new NotFiniteNumberException();

        return i.ToString();
    }
}
