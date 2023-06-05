// See https://aka.ms/new-console-template for more information
using MyCompiler;
using System.Text;

Console.WriteLine("Monkey REPL");
Console.WriteLine("Press ENTER on an empty line to end the source input.");

string source;
if (args.Length == 0)
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

    source = stringBuilder.ToString();
}
else
{
    source = await File.ReadAllTextAsync(args[0]);
}

var tokens = Lexer.ParseTokens(source);
var parser = new Parser(tokens);
var program = parser.ParseProgram();
if (!program.IsSuccess)
{
    Console.WriteLine("Parsing failed.");
    if (program.Error is AggregateException aggregateException)
    {
        foreach (var innerException in aggregateException.InnerExceptions)
        {
            Console.WriteLine(innerException);
        }
    }
    else
    {
        Console.WriteLine(program.Error);
    }

    return;
}

foreach (var statement in program.Value.Statements)
{
    Console.WriteLine(statement);
}


Console.WriteLine("Press ENTER to end.");
Console.ReadLine();
