// See https://aka.ms/new-console-template for more information
using MyCompiler;
using MyCompiler.Code;
using MyCompiler.Entities;
using MyCompiler.Vm;
using System.Text;

var env = EnvironmentStore.New();
var interpreter = new Interpreter();

Console.WriteLine("Monkey REPL");
Console.WriteLine("Type in your source code and press ENTER to execute.");
Console.WriteLine("Type \"\"\" at the start of a line to start multi-line parsing.");
Console.WriteLine(" - This is useful for pasting in large blocks of code.");
Console.WriteLine(" - End the multi-line parsing by typing \"\"\" at the start of a line again.");
Console.WriteLine("Press ENTER on an empty line to quit the REPL.");

var constants = new List<IObject>();
var globals = new IObject[Vm.GLOBALS_SIZE];
var symbolTable = new SymbolTable();

if (args.Length == 0)
{
    while (true)
    {
        Console.Write(">> ");
        var source = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(source))
            break;

        if (source == "\"\"\"")
        {
            var sb = new StringBuilder();
            while (true)
            {
                Console.Write("\"\" ");
                var line = Console.ReadLine();
                if (line == "\"\"\"")
                    break;

                sb.AppendLine(line);
            }

            source = sb.ToString();
        }

        //ExecuteInInterpreter(source, env);
        ExecuteInVm(source);
    }
}
else
{
    var source = await File.ReadAllTextAsync(args[0]);
    //ExecuteInInterpreter(source, env);
    ExecuteInVm(source);
}

void ExecuteInInterpreter(string source, EnvironmentStore env)
{
    var tokens = Lexer.ParseTokens(source);
    var parser = new Parser(tokens);
    var program = parser.ParseProgram();
    if (program.HasError)
    {
        PrintParserError(program.Error!);
        return;
    }

    var result = interpreter.Eval(program.Value, env);
    if (result.HasError)
    {
        Console.WriteLine($"Woops! We ran into some monkey business here!");
        Console.WriteLine($" interpreter errors:");
        Console.WriteLine($"\t{result.Error!.Message}");
        return;
    }

    var output = result.Value.Inspect();
    if (!string.IsNullOrEmpty(output))
    {
        Console.WriteLine(output);
    }
}

void ExecuteInVm(string source)
{
    var tokens = Lexer.ParseTokens(source);
    var parser = new Parser(tokens);
    var program = parser.ParseProgram();
    if (program.HasError)
    {
        PrintParserError(program.Error!);
        return;
    }

    var compiler = new Compiler(symbolTable, constants);
    var compilation = compiler.Compile(program.Value);
    if (compilation.HasError)
    {
        Console.WriteLine($"Woops! Compilation failed:");
        Console.WriteLine($"\t{compilation.Error!.Message}");
        return;
    }

    var machine = new Vm(compiler.Bytecode(), globals);
    var runtime = machine.Run();
    if (runtime.HasError)
    {
        Console.WriteLine($"Woops! Executing bytecode failed:");
        Console.WriteLine($"\t{runtime.Error!.Message}");
        return;
    }

    var lastPopped = machine.LastPoppedStackElem();
    Console.WriteLine(lastPopped.Inspect());
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
