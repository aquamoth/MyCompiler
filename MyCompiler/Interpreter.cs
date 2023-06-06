﻿using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler;

public class Interpreter
{
    public Result<IObject> Eval(IAstNode node, EnvironmentStore env)
    {
        return node switch
        {
            AstProgram program => EvalProgram(program.Statements, env),
            ExpressionStatement expression => Eval(expression.Expression, env),
            IntegerLiteral integer => new IntegerObject { Value = integer.Value },
            BooleanLiteral boolean => ToBooleanObject(boolean.Value),
            //NullLiteral _ => NullObject.Value,
            PrefixExpression prefix => EvalPrefixExpression(prefix, env),
            InfixExpression infix => EvalInfixExpression(infix, env),
            IfExpression @if => EvalIfExpression(@if, env),
            BlockStatement block => EvalStatements(block.Statements, env),
            ReturnStatement @return => EvalReturnStatement(@return, env),
            LetStatement let => EvalLetStatement(let, env),
            Identifier identifier => EvalIdentifier(identifier, env),
            FnExpression fn => EvalFunction(fn, env),
            CallExpression call => EvalCall(call, env),
            StringLiteral str => new StringObject { Value = str.Value },

            _ => new NotImplementedException($"Not yet evaluating {node}")
        };
    }

    private Result<IObject> EvalCall(CallExpression call, EnvironmentStore env)
    {
        var function = Eval(call.Function, env);
        if (!function.IsSuccess)
            return function;

        var args = EvalExpressions(call.Arguments, env);
        if (!args.IsSuccess)
            return args.Error!;

        return ApplyFunction(function.Value, args.Value);
    }

    private Result<IObject> ApplyFunction(IObject fn, IObject[] args)
    {
        if (fn is not FunctionObject function)
            return new Exception($"not a function: {fn.Type}");

        var extendedEnv = ExtendFunctionEnv(function, args);

        var evaluated = Eval(function.Body, extendedEnv);
        if (!evaluated.IsSuccess)
            return evaluated;

        return Result<IObject>.Success(
            UnwrapReturnValue(evaluated.Value)
        );
    }

    private static EnvironmentStore ExtendFunctionEnv(FunctionObject fn, IObject[] args)
    {
        var env = EnvironmentStore.NewEnclosed(fn.Env);
        foreach (var (param, value) in fn.Parameters.Zip(args))
        {
            env.Set(param.Value, value!);
        }

        return env;
    }

    private Result<IObject[]> EvalExpressions(IExpression[] arguments, EnvironmentStore env)
    {
        var results = new List<IObject>(arguments.Length);

        foreach (var arg in arguments)
        {
            var evaluated = Eval(arg, env);
            if (!evaluated.IsSuccess)
                return evaluated.Error!;

            results.Add(evaluated.Value);
        }

        return results.ToArray();
    }

    private Result<IObject> EvalFunction(FnExpression fn, EnvironmentStore env)
    {
        return new FunctionObject(fn.Parameters, fn.Body, env);
    }

    private static Result<IObject> EvalIdentifier(Identifier identifier, EnvironmentStore env)
    {
        return env.Get(identifier.Value);
    }

    private Result<IObject> EvalLetStatement(LetStatement let, EnvironmentStore env)
    {
        var value = Eval(let.Expression, env);
        if (!value.IsSuccess)
            return value.Error!;

        env.Set(let.Identifier.Value, value.Value);

        return NullObject.Value;
    }

    private Result<IObject> EvalIfExpression(IfExpression @if, EnvironmentStore env)
    {
        var condition = Eval(@if.Condition, env);
        if (!condition.IsSuccess)
            return condition;

        if (IsTruthy(condition.Value))
            return Eval(@if.Consequence, env);
        else if (@if.Alternative.HasValue)
            return Eval(@if.Alternative.Value, env);
        else
            return NullObject.Value;
    }

    private Result<IObject> EvalProgram(IEnumerable<IAstStatement> statements, EnvironmentStore env)
    {
        var result = EvalStatements(statements, env);
        if (!result.IsSuccess)
            return result;

        return Result<IObject>.Success(
            UnwrapReturnValue(result.Value)
        );
    }

    private Result<IObject> EvalStatements(IEnumerable<IAstStatement> statements, EnvironmentStore env)
    {
        IObject result = NullObject.Value;

        foreach (var statement in statements)
        {
            var value = Eval(statement, env);
            if (!value.IsSuccess)
                return value;

            result = value.Value;

            if (result is ReturnValue returnValue)
                return returnValue;
        }

        return Result<IObject>.Success(result);
    }

    private Result<IObject> EvalReturnStatement(ReturnStatement returnStatement, EnvironmentStore env)
    {
        var value = Eval(returnStatement.ReturnValue, env);
        if (!value.IsSuccess)
            return value;

        return new ReturnValue(value.Value);
    }


    private Result<IObject> EvalPrefixExpression(PrefixExpression prefix, EnvironmentStore env)
    {
        var right = Eval(prefix.Right, env);
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



    private Result<IObject> EvalInfixExpression(InfixExpression infix, EnvironmentStore env)
    {
        var left = Eval(infix.Left, env);
        if (!left.IsSuccess)
            return left;

        var right = Eval(infix.Right, env);
        if (!right.IsSuccess)
            return right;

        if (left.Value is IntegerObject leftInt && right.Value is IntegerObject rightInt)
            return EvalIntegerInfixExpression(infix.Operator, leftInt, rightInt);

        if (left.Value is StringObject leftStr && right.Value is StringObject rightStr)
            return EvalStringInfixExpression(infix.Operator, leftStr, rightStr);

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

    private Result<IObject> EvalStringInfixExpression(string @operator, StringObject leftStr, StringObject rightStr)
    {
        return @operator switch
        {
            "+" => new StringObject { Value = leftStr.Value + rightStr.Value },

            "<" => ToBooleanObject(leftStr.Value.CompareTo(rightStr.Value) == -1),
            ">" => ToBooleanObject(leftStr.Value.CompareTo(rightStr.Value) == 1),
            "==" => ToBooleanObject(leftStr.Value == rightStr.Value),
            "!=" => ToBooleanObject(leftStr.Value != rightStr.Value),

            _ => new Exception($"unknown operator: {leftStr.Type} {@operator} {rightStr.Type}")
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

    private static IObject UnwrapReturnValue(IObject value)
    {
        return value is ReturnValue returnValue
            ? returnValue.Value
            : value;
    }
}
