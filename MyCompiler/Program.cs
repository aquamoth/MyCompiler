// See https://aka.ms/new-console-template for more information
using MyCompiler;
using System.Text;

var interpreter = new Interpreter();

Console.WriteLine("Monkey REPL");
Console.WriteLine("Press ENTER on an empty line to end the source input.");

if (args.Length == 0)
{
    while (true)
    {
        StringBuilder stringBuilder = new();

        string line;
        do
        {
            Console.Write(">> ");
            line = Console.ReadLine() ?? "";
            stringBuilder.AppendLine(line);
        }
        while (line != "");

        var source = stringBuilder.ToString();
        if (string.IsNullOrWhiteSpace(source))
            break;

        Execute(source);
    }
}
else
{
    var source = await File.ReadAllTextAsync(args[0]);
    Execute(source);
}

void Execute(string source)
{
    var tokens = Lexer.ParseTokens(source);
    var parser = new Parser(tokens);
    var program = parser.ParseProgram();
    if (!program.IsSuccess)
    {
        PrintParserError(program.Error!);
        return;
    }
    
    var result = interpreter.Eval(program.Value);
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Woops! We ran into some monkey business here!");
        Console.WriteLine($" interpreter errors:");
        Console.WriteLine($"\t{result.Error!.Message}");
        return;
    }

    Console.WriteLine(result.Value.Inspect());
}

void PrintParserError(Exception error)
{
    if (error is AggregateException aggregateException)
    {
        PrintParserErrors(aggregateException.InnerExceptions.ToArray());
    }
    else
    {
        PrintParserErrors(error);
    }
}

void PrintParserErrors(params Exception[] errors)
{
    Console.WriteLine("Woops! We ran into some monkey business here!");
    Console.WriteLine(" parser errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($"\t{error.Message}");
    }
}
