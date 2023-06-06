using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.Data.SqlTypes;

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
            return ToBooleanObject(boolean.Value);

        //if (node is NullLiteral)
        //    return NullObject.Value;

        if (node is PrefixExpression prefix)
            return EvalPrefixExpression(prefix);

        if (node is InfixExpression infix)
            return EvalInfixExpression(infix);

        if (node is IfExpression ifExpression)
            return EvalIfExpression(ifExpression);

        if (node is BlockStatement block)
            return EvalStatements(block.Statements);

        return new NotImplementedException($"Not yet evaluating {node}");
    }

    private Result<IObject> EvalIfExpression(IfExpression ifExpression)
    {
        var condition = Eval(ifExpression.Condition);
        if (!condition.IsSuccess)
            return condition;

        if (IsTruthy(condition.Value))
            return Eval(ifExpression.Consequence);
        else if (ifExpression.Alternative.HasValue)
            return Eval(ifExpression.Alternative.Value);
        else
            return NullObject.Value;
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
            return ToBooleanObject(!boolean.Value);

        if (value is NullObject)
            return BooleanObject.True;

        //if (value is IntegerObject integer)
        //    return NativeBoolToBooleanObject(integer.Value != 0);

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

        if (left.Value is BooleanObject leftBool && right.Value is BooleanObject rightBool)
            return EvalBooleanInfixExpression(infix.Operator, leftBool, rightBool);

        return NullObject.Value; //TODO:???
    }

    private Result<IObject> EvalIntegerInfixExpression(string @operator, IntegerObject leftInt, IntegerObject rightInt)
    {
        IObject result = @operator switch
        {
            "+" => new IntegerObject { Value = leftInt.Value + rightInt.Value },
            "-" => new IntegerObject { Value = leftInt.Value - rightInt.Value },
            "*" => new IntegerObject { Value = leftInt.Value * rightInt.Value },
            "/" => new IntegerObject { Value = leftInt.Value / rightInt.Value },

            "<" => ToBooleanObject(leftInt.Value < rightInt.Value),
            ">" => ToBooleanObject(leftInt.Value > rightInt.Value),
            "==" => ToBooleanObject(leftInt.Value == rightInt.Value),
            "!=" => ToBooleanObject(leftInt.Value != rightInt.Value),

            _ => NullObject.Value,//TODO:???
        };

        return Result<IObject>.Success(result);
    }

    private Result<IObject> EvalBooleanInfixExpression(string @operator, BooleanObject leftBool, BooleanObject rightBool)
    {
        IObject result = @operator switch
        {
            "==" => ToBooleanObject(leftBool == rightBool),
            "!=" => ToBooleanObject(leftBool != rightBool),

            _ => NullObject.Value,//TODO:???
        };

        return Result<IObject>.Success(result);
    }

    private static BooleanObject ToBooleanObject(bool value) => value ? BooleanObject.True : BooleanObject.False;

    private static bool IsTruthy(IObject condition)
    {
        if (condition == NullObject.Value) return false;
        if (condition == BooleanObject.True) return true;
        if (condition == BooleanObject.False) return false;
        return true;//TODO: 0 -> false!
    }
}
