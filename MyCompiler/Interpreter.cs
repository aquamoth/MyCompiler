using Microsoft.Extensions.Logging;
using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler;

public class Interpreter
{
    private readonly EnvironmentStore builtin;
    private readonly ILogger? logger;

    public Interpreter(ILogger? logger = null)
    {
        builtin = EnvironmentStore.New();
        builtin.Set("len", BuiltIns.GetByName("len").Value);
        builtin.Set("first", BuiltIns.GetByName("first").Value);
        builtin.Set("last", BuiltIns.GetByName("last").Value);
        builtin.Set("rest", BuiltIns.GetByName("rest").Value);
        builtin.Set("push", BuiltIns.GetByName("push").Value);
        builtin.Set("puts", BuiltIns.GetByName("puts").Value);
        builtin.Set("gets", BuiltIns.GetByName("gets").Value);
        this.logger = logger;
    }

    public Maybe<IObject> Eval(IAstNode node, EnvironmentStore env)
    {
        return node switch
        {
            AstProgram program => EvalProgram(program.Statements, env),
            ExpressionStatement expression => Eval(expression.Expression, env),
            IntegerLiteral integer => new IntegerObject(integer.Value),
            BooleanLiteral boolean => ToBooleanObject(boolean.Value),
            //NullLiteral _ => NullObject.Value,
            PrefixExpression prefix => EvalPrefixExpression(prefix, env),
            InfixExpression infix => EvalInfixExpression(infix, env),
            IfExpression @if => EvalIfExpression(@if, env),
            BlockStatement block => EvalStatements(block.Statements, env),
            ReturnStatement @return => EvalReturnStatement(@return, env),
            LetStatement let => EvalLetStatement(let, env),
            Identifier identifier => EvalIdentifier(identifier, env),
            FunctionLiteral fn => EvalFunction(fn, env),
            CallExpression call => EvalCall(call, env),
            StringLiteral str => new StringObject(str.Value),
            ArrayExpression array => EvalArrayExpression(array, env),
            IndexExpression index => EvalIndexExpression(index, env),
            HashLiteral hash => EvalHashLiteral(hash, env),

            _ => new NotImplementedException($"Not yet evaluating {node}")
        };
    }

    private Maybe<IObject> EvalCall(CallExpression call, EnvironmentStore env)
    {
        var function = Eval(call.Function, env);
        if (function.HasError)
            return function;

        var args = EvalExpressions(call.Arguments, env);
        if (args.HasError)
            return args.Error!;

        return ApplyFunction(function.Value, args.Value);
    }

    private Maybe<IObject> ApplyFunction(IObject fn, IObject[] args)
    {
        if (fn is FunctionObject function)
        {
            var extendedEnv = ExtendFunctionEnv(function, args);

            var evaluated = Eval(function.Body, extendedEnv);
            if (evaluated.HasError)
                return evaluated;

            return Maybe<IObject>.From(
                UnwrapReturnValue(evaluated.Value)
            );
        }

        if (fn is BuiltIn builtin)
            return builtin.Fn(args);

        return new Exception($"not a function: {fn.Type}");
    }

    private static EnvironmentStore ExtendFunctionEnv(FunctionObject fn, IObject[] args)
    {
        var env = EnvironmentStore.NewEnclosed(fn.Env);
        foreach (var (param, value) in fn.Parameters.Zip(args))
        {
            env.Set(param.Name, value!);
        }

        return env;
    }

    private Maybe<IObject[]> EvalExpressions(IExpression[] arguments, EnvironmentStore env)
    {
        var results = new List<IObject>(arguments.Length);

        foreach (var arg in arguments)
        {
            var evaluated = Eval(arg, env);
            if (evaluated.HasError)
                return evaluated.Error!;

            results.Add(evaluated.Value);
        }

        return results.ToArray();
    }

    private Maybe<IObject> EvalFunction(FunctionLiteral fn, EnvironmentStore env)
    {
        return new FunctionObject(fn.Parameters, fn.Body, env);
    }

    private Maybe<IObject> EvalIdentifier(Identifier identifier, EnvironmentStore env)
    {
        var result = env.Get(identifier.Name);
        if (result.HasValue)
            return result;

        return builtin.Get(identifier.Name);
    }

    private Maybe<IObject> EvalLetStatement(LetStatement let, EnvironmentStore env)
    {
        var value = Eval(let.Expression, env);
        if (value.HasError)
            return value.Error!;

        env.Set(let.Identifier.Name, value.Value);

        return NullObject.Value;
    }

    private Maybe<IObject> EvalIfExpression(IfExpression @if, EnvironmentStore env)
    {
        var condition = Eval(@if.Condition, env);
        if (condition.HasError)
            return condition;

        if (IsTruthy(condition.Value))
            return Eval(@if.Consequence, env);
        else if (@if.Alternative.HasValue)
            return Eval(@if.Alternative.Value, env);
        else
            return NullObject.Value;
    }

    private Maybe<IObject> EvalArrayExpression(ArrayExpression array, EnvironmentStore env)
    {
        var elements = EvalExpressions(array.Elements, env);
        if (elements.HasError)
            return elements.Error!;

        return new ArrayObject(elements.Value);
    }
    private Maybe<IObject> EvalHashLiteral(HashLiteral hash, EnvironmentStore env)
    {
        var hashObj = new HashObject();

        foreach (var (key, value) in hash.Pairs)
        {
            var keyEval = Eval(key, env);
            if (keyEval.HasError)
                return keyEval.Error!;

            if (keyEval.Value is not IHashable hashable)
                return new Exception($"unusable as hash key: {keyEval.Value.Type}");

            var valueEval = Eval(value, env);
            if (valueEval.HasError)
                return valueEval.Error!;

            var keyHash = hashable.HashKey();

            hashObj.Pairs[keyHash] = new HashPair(keyEval.Value, valueEval.Value);
        }

        return hashObj;
    }

    private Maybe<IObject> EvalIndexExpression(IndexExpression indexExpr, EnvironmentStore env)
    {
        var left = Eval(indexExpr.Left, env);
        if (left.HasError)
            return left;

        switch (left.Value)
        {
            case ArrayObject array:
                {
                    var right = Eval(indexExpr.Index, env);
                    if (right.HasError)
                        return right;

                    if (right.Value is not IntegerObject index)
                        return new Exception($"index value {right.Value.Type} is not supported {Parser.ExceptionLocatorString(indexExpr.Index.Token)}");

                    if (index.Value < 0 || index.Value >= array.Elements.Length)
                        return new Exception($"index {index.Value} is out of range {Parser.ExceptionLocatorString(indexExpr.Index.Token)}");

                    return Maybe<IObject>.From(array.Elements[index.Value]);
                }

            case HashObject hash:
                {
                    var right = Eval(indexExpr.Index, env);
                    if (right.HasError)
                        return right;

                    if (right.Value is not IHashable hashable)
                        return new Exception($"unusable as hash key: {right.Value.Type} {Parser.ExceptionLocatorString(indexExpr.Index.Token)}");

                    if (hash.Pairs.TryGetValue(hashable.HashKey(), out var hashPair))
                        return Maybe<IObject>.From(hashPair.Value);
                    else
                        return NullObject.Value;
                }

            default:
                return new Exception($"index operator is not supported on {left.Value.Type} {Parser.ExceptionLocatorString(indexExpr.Token)}");
        }
    }



    private Maybe<IObject> EvalProgram(IEnumerable<IAstStatement> statements, EnvironmentStore env)
    {
        var result = EvalStatements(statements, env);
        if (result.HasError)
        {
            logger?.LogCritical(result.Error!.Message);
            return result;
        }

        return Maybe<IObject>.From(
            UnwrapReturnValue(result.Value)
        );
    }

    private Maybe<IObject> EvalStatements(IEnumerable<IAstStatement> statements, EnvironmentStore env)
    {
        IObject result = NullObject.Value;

        foreach (var statement in statements)
        {
            var value = Eval(statement, env);
            if (value.HasError)
                return value;

            result = value.Value;

            if (result is ReturnValue returnValue)
                return returnValue;
        }

        return Maybe<IObject>.From(result);
    }

    private Maybe<IObject> EvalReturnStatement(ReturnStatement returnStatement, EnvironmentStore env)
    {
        var value = Eval(returnStatement.ReturnValue, env);
        if (value.HasError)
            return value;

        return new ReturnValue(value.Value);
    }


    private Maybe<IObject> EvalPrefixExpression(PrefixExpression prefix, EnvironmentStore env)
    {
        var right = Eval(prefix.Right, env);
        if (right.HasError)
            return right;

        return prefix.Operator switch
        {
            "!" => EvalBangOperatorExpression(right.Value),
            "-" => EvalMinusPrefixOperatorExpression(right.Value),
            _ => new Exception($"unknown operator: {prefix.Operator}{right.Value.Type}")
        };
    }

    private static Maybe<IObject> EvalBangOperatorExpression(IObject value)
    {
        if (value is BooleanObject boolean)
            return ToBooleanObject(!boolean.Value);

        if (value is NullObject)
            return BooleanObject.True;

        //if (value is IntegerObject integer)
        //    return NativeBoolToBooleanObject(integer.Value != 0);

        return BooleanObject.False; //TODO:???
    }

    private static Maybe<IObject> EvalMinusPrefixOperatorExpression(IObject value)
    {
        if (value is IntegerObject integer)
            return new IntegerObject(-integer.Value);

        return new Exception($"unknown operator: -{value.Type}");
    }



    private Maybe<IObject> EvalInfixExpression(InfixExpression infix, EnvironmentStore env)
    {
        var left = Eval(infix.Left, env);
        if (left.HasError)
            return left;

        var right = Eval(infix.Right, env);
        if (right.HasError)
            return right;

        if (left.Value is IntegerObject leftInt && right.Value is IntegerObject rightInt)
            return EvalIntegerInfixExpression(infix.Operator, leftInt, rightInt);

        if (left.Value is StringObject leftStr && right.Value is StringObject rightStr)
            return EvalStringInfixExpression(infix.Operator, leftStr, rightStr);

        if (left.Value is BooleanObject leftBool && right.Value is BooleanObject rightBool)
            return EvalBooleanInfixExpression(infix.Operator, leftBool, rightBool);

        return new Exception($"type mismatch: {left.Value.Type} {infix.Operator} {right.Value.Type}");
    }

    private Maybe<IObject> EvalIntegerInfixExpression(string @operator, IntegerObject leftInt, IntegerObject rightInt)
    {
        return @operator switch
        {
            "+" => new IntegerObject(leftInt.Value + rightInt.Value),
            "-" => new IntegerObject(leftInt.Value - rightInt.Value),
            "*" => new IntegerObject(leftInt.Value * rightInt.Value),
            "/" => new IntegerObject(leftInt.Value / rightInt.Value),

            "<" => ToBooleanObject(leftInt.Value < rightInt.Value),
            ">" => ToBooleanObject(leftInt.Value > rightInt.Value),
            "==" => ToBooleanObject(leftInt.Value == rightInt.Value),
            "!=" => ToBooleanObject(leftInt.Value != rightInt.Value),

            _ => new Exception($"unknown operator: {leftInt.Type} {@operator} {rightInt.Type}")
        };
    }

    private Maybe<IObject> EvalStringInfixExpression(string @operator, StringObject leftStr, StringObject rightStr)
    {
        return @operator switch
        {
            "+" => new StringObject(leftStr.Value + rightStr.Value),

            "<" => ToBooleanObject(leftStr.Value.CompareTo(rightStr.Value) == -1),
            ">" => ToBooleanObject(leftStr.Value.CompareTo(rightStr.Value) == 1),
            "==" => ToBooleanObject(leftStr.Value == rightStr.Value),
            "!=" => ToBooleanObject(leftStr.Value != rightStr.Value),

            _ => new Exception($"unknown operator: {leftStr.Type} {@operator} {rightStr.Type}")
        };
    }

    private Maybe<IObject> EvalBooleanInfixExpression(string @operator, BooleanObject leftBool, BooleanObject rightBool)
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
