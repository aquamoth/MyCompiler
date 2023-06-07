using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler;

public class Interpreter
{
    private readonly EnvironmentStore builtin;
    public Interpreter()
    {
        builtin = EnvironmentStore.New();
        builtin.Set("len", new BuiltIn(BuiltIn_Len));
        builtin.Set("first", new BuiltIn(BuiltIn_First));
        builtin.Set("last", new BuiltIn(BuiltIn_Last));
    }

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
            ArrayExpression array => EvalArrayExpression(array, env),
            IndexExpression index => EvalIndexExpression(index, env),

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
        if (fn is FunctionObject function)
        {
            var extendedEnv = ExtendFunctionEnv(function, args);

            var evaluated = Eval(function.Body, extendedEnv);
            if (!evaluated.IsSuccess)
                return evaluated;

            return Result<IObject>.Success(
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

    private Result<IObject> EvalIdentifier(Identifier identifier, EnvironmentStore env)
    {
        var result = env.Get(identifier.Value);
        if (result.IsSuccess)
            return result;

        return builtin.Get(identifier.Value);
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

    private Result<IObject> EvalArrayExpression(ArrayExpression array, EnvironmentStore env)
    {
        var elements = EvalExpressions(array.Elements, env);
        if (!elements.IsSuccess)
            return elements.Error!;

        return new ArrayLiteral(elements.Value);
    }

    private Result<IObject> EvalIndexExpression(IndexExpression indexExpr, EnvironmentStore env)
    {
        var left = Eval(indexExpr.Left, env);
        if (!left.IsSuccess)
            return left;

        if (left.Value is not ArrayLiteral array)
            return new Exception($"index operator is not supported on {left.Value.Type} {Parser.ExceptionLocatorString(indexExpr.Token)}");

        var right = Eval(indexExpr.Right, env);
        if (!right.IsSuccess)
            return right;

        if (right.Value is not IntegerObject index)
            return new Exception($"index value {right.Value.Type} is not supported {Parser.ExceptionLocatorString(indexExpr.Right.Token)}");

        if (index.Value < 0 || index.Value >= array.Elements.Length)
            return new Exception($"index {index.Value} is out of range {Parser.ExceptionLocatorString(indexExpr.Right.Token)}");

        return Result<IObject>.Success(array.Elements[index.Value]);
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

    private static Result<IObject> BuiltIn_Len(IObject[] args)
    {
        if (args.Length != 1)
            return new Exception($"wrong number of arguments. got={args.Length}, want=1");

        return args[0] switch
        {
            StringObject arg0 => new IntegerObject { Value = arg0.Value.Length },
            ArrayLiteral arg0 => new IntegerObject { Value = arg0.Elements.Length },

            _ => new Exception($"Expected {ObjectType.STRING} or {ObjectType.ARRAY} but got {args[0].Type}")
        };
    }

    private static Result<IObject> BuiltIn_First(IObject[] args)
    {
        if (args.Length != 1)
            return new Exception($"wrong number of arguments. got={args.Length}, want=1");

        return args[0] switch
        {
            ArrayLiteral arg0 => Result<IObject>.Success(arg0.Elements.Length == 0 ? NullObject.Value : arg0.Elements[0]),

            _ => new Exception($"Expected {ObjectType.ARRAY} but got {args[0].Type}")
        };
    }

    private static Result<IObject> BuiltIn_Last(IObject[] args)
    {
        if (args.Length != 1)
            return new Exception($"wrong number of arguments. got={args.Length}, want=1");

        return args[0] switch
        {
            ArrayLiteral arg0 => Result<IObject>.Success(arg0.Elements.Length == 0 ? NullObject.Value : arg0.Elements[arg0.Elements.Length - 1]),

            _ => new Exception($"Expected {ObjectType.ARRAY} but got {args[0].Type}")
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
