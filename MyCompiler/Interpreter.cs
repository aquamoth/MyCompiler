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

        return infix.Operator switch
        {
            "+" => EvalPlusInfixOperatorExpression(left.Value, right.Value),
            "-" => EvalMinusInfixOperatorExpression(left.Value, right.Value),
            "*" => EvalAsteriskInfixOperatorExpression(left.Value, right.Value),
            "/" => EvalForwardSlashInfixOperatorExpression(left.Value, right.Value),
            _ => NullObject.Value, //TODO:???
        };
    }

    private static Result<IObject> EvalPlusInfixOperatorExpression(IObject left, IObject right)
    {
        if (left is not IntegerObject leftInt || right is not IntegerObject rightInt)
            return NullObject.Value;

        return new IntegerObject { Value = leftInt.Value + rightInt.Value };
    }

    private static Result<IObject> EvalMinusInfixOperatorExpression(IObject left, IObject right)
    {
        if (left is not IntegerObject leftInt || right is not IntegerObject rightInt)
            return NullObject.Value;

        return new IntegerObject { Value = leftInt.Value - rightInt.Value };
    }

    private static Result<IObject> EvalAsteriskInfixOperatorExpression(IObject left, IObject right)
    {
        if (left is not IntegerObject leftInt || right is not IntegerObject rightInt)
            return NullObject.Value;

        return new IntegerObject { Value = leftInt.Value * rightInt.Value };
    }

    private static Result<IObject> EvalForwardSlashInfixOperatorExpression(IObject left, IObject right)
    {
        if (left is not IntegerObject leftInt || right is not IntegerObject rightInt)
            return NullObject.Value;

        return new IntegerObject { Value = leftInt.Value / rightInt.Value };
    }
}
