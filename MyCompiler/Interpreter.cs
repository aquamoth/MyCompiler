using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler;

public class Interpreter
{
    public Result<IObject> Eval(IAstNode node)
    {
        if (node is AstProgram program)
            return EvalProgram(program);

        if(node is ExpressionStatement statement)
            return Eval(statement.Expression);

        if (node is IntegerLiteral integer)
            return new IntegerObject { Value = integer.Value };

        if (node is BooleanLiteral boolean)
            return new BooleanObject { Value = boolean.Value };



        return NullObject.Value;
    }

    private Result<IObject> EvalProgram(AstProgram program)
    {
        IObject result = NullObject.Value;

        foreach (var statement in program.Statements)
        {
            var value = Eval(statement);
            if (!value.IsSuccess)
                return value;

            result = value.Value;
        }

        return Result<IObject>.Success(result);
    }
}
