using MyCompiler.Code;

namespace MyCompiler.Tests;

public class SymbolTable_
{
    [Fact]
    public void Resolves_global_and_local_scope()
    {
        var expected = new[]
        {
            new Symbol("a", Symbol.GLOBAL_SCOPE, 0),
            new Symbol("b", Symbol.GLOBAL_SCOPE, 1),
            new Symbol("c", Symbol.LOCAL_SCOPE, 0),
            new Symbol("d", Symbol.LOCAL_SCOPE, 1),
        };


        var global = new SymbolTable();
        global.Define("a");
        global.Define("b");

        var local = new SymbolTable(global);
        local.Define("c");
        local.Define("d");

        var actual = expected.Select(s => local.Resolve(s.Name).Value);


        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Resolve_nested_local_scopes()
    {
        var expectedFirst = new[]
        {
            new Symbol("a", Symbol.GLOBAL_SCOPE, 0),
            new Symbol("b", Symbol.GLOBAL_SCOPE, 1),
            new Symbol("c", Symbol.LOCAL_SCOPE, 0),
            new Symbol("d", Symbol.LOCAL_SCOPE, 1),
        };

        var expectedSecond = new[]
        {
            new Symbol("a", Symbol.GLOBAL_SCOPE, 0),
            new Symbol("b", Symbol.GLOBAL_SCOPE, 1),
            new Symbol("c", Symbol.LOCAL_SCOPE, 0),
            new Symbol("d", Symbol.LOCAL_SCOPE, 1),
            new Symbol("e", Symbol.LOCAL_SCOPE, 0),
            new Symbol("f", Symbol.LOCAL_SCOPE, 1),
        };


        var global = new SymbolTable();
        global.Define("a");
        global.Define("b");

        var firstLocal = new SymbolTable(global);
        firstLocal.Define("c");
        firstLocal.Define("d");

        var secondLocal = new SymbolTable(firstLocal);
        secondLocal.Define("e");
        secondLocal.Define("f");

        var actualFirst = expectedFirst.Select(s => firstLocal.Resolve(s.Name).Value);
        var actualSecond = expectedSecond.Select(s => secondLocal.Resolve(s.Name).Value);


        Assert.Equal(expectedFirst, actualFirst);
        Assert.Equal(expectedSecond, actualSecond);
    }
}
