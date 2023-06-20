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
            new Symbol("c", Symbol.FREE_SCOPE, 0),
            new Symbol("d", Symbol.FREE_SCOPE, 1),
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


    [Fact]
    public void Errors_resolving_unresolvable_free()
    {
        var expectedSecond = new[]
        {
            new Symbol("a", Symbol.GLOBAL_SCOPE, 0),
            new Symbol("c", Symbol.FREE_SCOPE, 0),
            new Symbol("e", Symbol.LOCAL_SCOPE, 0),
            new Symbol("f", Symbol.LOCAL_SCOPE, 1),
        };

        var global = new SymbolTable();
        global.Define("a");

        var firstLocal = new SymbolTable(global);
        firstLocal.Define("c");

        var secondLocal = new SymbolTable(firstLocal);
        secondLocal.Define("e");
        secondLocal.Define("f");

        var actualSecond = expectedSecond.Select(s => secondLocal.Resolve(s.Name).Value);

        var actual_b = secondLocal.Resolve("b");
        var actual_d = secondLocal.Resolve("d");

        Assert.Equal(expectedSecond, actualSecond);
        Assert.False(actual_b.HasValue, "b should not be resolvable");
        Assert.False(actual_d.HasValue, "d should not be resolvable");
    }


    [Fact]
    public void Resolve_builtins()
    {
        var expectedSymbols = new[]
        {
            new Symbol("a", Symbol.BUILTIN_SCOPE, 0),
            new Symbol("c", Symbol.BUILTIN_SCOPE, 1),
            new Symbol("e", Symbol.BUILTIN_SCOPE, 2),
            new Symbol("f", Symbol.BUILTIN_SCOPE, 3),
        };

        var global = new SymbolTable();
        var firstLocal = new SymbolTable(global);
        var secondLocal = new SymbolTable(firstLocal);

        foreach((var name, var index) in expectedSymbols.Select((s, i) => (s.Name, i)))
        {
            global.DefineBuiltin(index, name);
        }

        foreach(var tbl in new[] { global, firstLocal, secondLocal })
        {
            foreach(var expected in expectedSymbols)
            {
                var actual = tbl.Resolve(expected.Name);
                Assert.True(actual.HasValue, actual.Error?.Message);
                Assert.Equal(expected, actual);
            }
        }
    }
}
