using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.Data.SqlTypes;

namespace MyCompiler;

public class Interpreter
{
    public Result<IObject> Eval(IAstNode node)
    {
        return node switch
        {
            AstProgram program => EvalProgram(program.Statements),
            ExpressionStatement expression => Eval(expression.Expression),
            IntegerLiteral integer => new IntegerObject { Value = integer.Value },
            BooleanLiteral boolean => ToBooleanObject(boolean.Value),
            //NullLiteral _ => NullObject.Value,
            PrefixExpression prefix => EvalPrefixExpression(prefix),
            InfixExpression infix => EvalInfixExpression(infix),
            IfExpression @if => EvalIfExpression(@if),
            BlockStatement block => EvalStatements(block.Statements),
            ReturnStatement @return => EvalReturnStatement(@return),
            //LetStatement letStatement => EvalLetStatement(letStatement),

            _ => new NotImplementedException($"Not yet evaluating {node}")
        };
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

    private Result<IObject> EvalProgram(IEnumerable<IAstStatement> statements)
    {
        var result = EvalStatements(statements);
        if (!result.IsSuccess)
            return result;

        if (result.Value is ReturnValue returnValue)
            return Result<IObject>.Success(returnValue.Value);

        return result;
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

            if (result is ReturnValue returnValue)
                return returnValue;
        }

        return Result<IObject>.Success(result);
    }

    private Result<IObject> EvalReturnStatement(ReturnStatement returnStatement)
    {
        var value = Eval(returnStatement.ReturnValue);
        if (!value.IsSuccess)
            return value;

        return new ReturnValue(value.Value);
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
            _ => new Exception($"unknown operator: {prefix.Operator}{right.Value.Type}")
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

        return new Exception($"unknown operator: -{value.Type}");
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

        return new Exception($"type mismatch: {left.Value.Type} {infix.Operator} {right.Value.Type}");
    }

    private Result<IObject> EvalIntegerInfixExpression(string @operator, IntegerObject leftInt, IntegerObject rightInt)
    {
        return @operator switch
        {
            "+" => new IntegerObject { Value = leftInt.Value + rightInt.Value },
            "-" => new IntegerObject { Value = leftInt.Value - rightInt.Value },
            "*" => new IntegerObject { Value = leftInt.Value * rightInt.Value },
            "/" => new IntegerObject { Value = leftInt.Value / rightInt.Value },

            "<" => ToBooleanObject(leftInt.Value < rightInt.Value),
            ">" => ToBooleanObject(leftInt.Value > rightInt.Value),
            "==" => ToBooleanObject(leftInt.Value == rightInt.Value),
            "!=" => ToBooleanObject(leftInt.Value != rightInt.Value),

            _ => new Exception($"unknown operator: {leftInt.Type} {@operator} {rightInt.Type}")
        };
    }

    private Result<IObject> EvalBooleanInfixExpression(string @operator, BooleanObject leftBool, BooleanObject rightBool)
    {
        return @operator switch
        {
            "==" => ToBooleanObject(leftBool == rightBool),
            "!=" => ToBooleanObject(leftBool != rightBool),

            _ => new Exception($"unknown operator: {leftBool.Type} {@operator} {rightBool.Type}")
        };
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
