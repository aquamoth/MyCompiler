using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler;

public class Interpreter
{
    public Result<IObject> Eval(IAstNode node)
    {
        if (node is AstProgram program)
            return EvalStatements(program.Statements);

        if (node is ExpressionStatement statement)
            return Eval(statement.Expression);

        if (node is IntegerLiteral integer)
            return new IntegerObject { Value = integer.Value };

        if (node is BooleanLiteral boolean)
            return EvalBooleanLiteral(boolean);

        //if (node is NullLiteral)
        //    return NullObject.Value;

        if (node is PrefixExpression prefix)
            return EvalPrefixExpression(prefix);

        if (node is InfixExpression infix)
            return EvalInfixExpression(infix);

        return new NotImplementedException($"Not yet evaluating {node}");
    }

    private Result<IObject> EvalStatements(IEnumerable<IAstStatement> statements)
    {
        IObject result = NullObject.Value;

        foreach (var statement in statements)
        {
            var value = Eval(statement);
            if (!value.IsSuccess)
                return value;

            result = value.Value;
        }

        return Result<IObject>.Success(result);
    }

    private static Result<IObject> EvalBooleanLiteral(BooleanLiteral boolean)
    {
        return boolean.Value
            ? BooleanObject.True
            : BooleanObject.False;
    }




    private Result<IObject> EvalPrefixExpression(PrefixExpression prefix)
    {
        var right = Eval(prefix.Right);
        if (!right.IsSuccess)
            return right;

        return prefix.Operator switch
        {
            "!" => EvalBangOperatorExpression(right.Value),
            "-" => EvalMinusPrefixOperatorExpression(right.Value),
            _ => NullObject.Value, //TODO:???
        };
    }

    private static Result<IObject> EvalBangOperatorExpression(IObject value)
    {
        if (value is BooleanObject boolean)
            return boolean.Value ? BooleanObject.False : BooleanObject.True;

        if (value is NullObject)
            return BooleanObject.True;

        //if (value is IntegerObject integer)
        //    return integer.Value == 0 ? BooleanObject.False : BooleanObject.True;

        return BooleanObject.False; //TODO:???
    }

    private static Result<IObject> EvalMinusPrefixOperatorExpression(IObject value)
    {
        if (value is IntegerObject integer)
            return new IntegerObject { Value = -integer.Value };

        return NullObject.Value;//TODO:???
    }



    private Result<IObject> EvalInfixExpression(InfixExpression infix)
    {
        var left = Eval(infix.Left);
        if (!left.IsSuccess)
            return left;

        var right = Eval(infix.Right);
        if (!right.IsSuccess)
            return right;

        if (left.Value is IntegerObject leftInt && right.Value is IntegerObject rightInt)
            return EvalIntegerInfixExpression(infix.Operator, leftInt, rightInt);

        return NullObject.Value; //TODO:???
    }

    private Result<IObject> EvalIntegerInfixExpression(string @operator, IntegerObject leftInt, IntegerObject rightInt)
    {
        return @operator switch
        {
            "+" => new IntegerObject { Value = leftInt.Value + rightInt.Value },
            "-" => new IntegerObject { Value = leftInt.Value - rightInt.Value },
            "*" => new IntegerObject { Value = leftInt.Value * rightInt.Value },
            "/" => new IntegerObject { Value = leftInt.Value / rightInt.Value },
            _ => NullObject.Value,//TODO:???
        };
    }
}
