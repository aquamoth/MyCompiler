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


foreach (var token in Lexer.ParseTokens(source))
{
    var keyword = source[token.Position..(token.Position+token.Length)];
    Console.WriteLine($"Type:{token.Type} Literal:{keyword}");
}


Console.WriteLine("Press ENTER to end.");
Console.ReadLine();
