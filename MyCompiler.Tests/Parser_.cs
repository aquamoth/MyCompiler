using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace MyCompiler.Tests
{
    public class Parser_
    {
        private readonly ITestOutputHelper outputHelper;

        public Parser_(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void Parses_let_statements()
        {
            using var logger = new XUnitLogger<Parser>(outputHelper);

            string source = """
                let x = 5;
                let y = 10;
                let foobar = 838383;
                """;

            Parser parser = new(Lexer.ParseTokens(source), logger);

            var program = parser.ParseProgram();

            Assert.True(program.IsSuccess);

            Assert.Collection(program.Value.Statements,
                s => { },
                s => { },
                s => { }
            );
        }
    }
}