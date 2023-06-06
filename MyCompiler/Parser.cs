using Microsoft.Extensions.Logging;
using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.Reflection.Metadata.Ecma335;

namespace MyCompiler;

public class Parser
{
    private readonly IEnumerator<Token> tokenEnumerator;
    private readonly ILogger? logger;
    private Token currentToken;

    public Parser(IEnumerable<Token> tokenSource, ILogger? logger = null)
    {
        tokenEnumerator = tokenSource.GetEnumerator();
        this.logger = logger;
        this.currentToken = Token.From(Tokens.Illegal, "", 0, 0, 0, 0);
    }

    public Result<AstProgram> ParseProgram()
    {
        var program = new AstProgram();

        AdvanceToken();
        AdvanceToken();

        var allErrors = new List<Exception>();

        while (currentToken.Type != Tokens.EndOfFile)
        {
            var statement = ParseStatement();
            if (statement.IsSuccess)
            {
                program.Statements.Add(statement.Value);
            }
            else
            {
                logger?.LogCritical(statement.Error, "Error parsing source!");
                allErrors.Add(statement.Error!);
            }

            AdvanceToken();
        }

        return allErrors.Any()
            ? new AggregateException(allErrors)
            : program;
    }

    private Result<IAstStatement> ParseStatement()
    {
        return currentToken.Type switch
        {
            Tokens.Let => ParseLetStatement(),
            Tokens.Return => ParseReturnStatement(),
            Tokens.Semicolon => new EmptyStatement { Token = currentToken },
            _ => ParseExpressionStatement()
        };
    }

    private Result<IAstStatement> ParseLetStatement()
    {
        var letToken = currentToken;

        var identifierToken = AdvanceTokenIf(Tokens.Identifier);
        if (!identifierToken.IsSuccess)
            return identifierToken.Error!;

        var identifier = new Identifier { Token = currentToken, Value = identifierToken.Value.Literal };

        var assignmentToken = AdvanceTokenIf(Tokens.Assign);
        if (!assignmentToken.IsSuccess)
            return assignmentToken.Error!;

        AdvanceToken();

        var expression = ParseExpression();
        if (!expression.IsSuccess)
            return expression.Error!;


        AdvanceTokenIf(Tokens.Semicolon);

        return new LetStatement { Token = letToken, Identifier = identifier, Expression = expression.Value };
    }

    private Result<IAstStatement> ParseReturnStatement()
    {
        var returnToken = currentToken;

        AdvanceToken();

        var expression = ParseExpression();
        if (!expression.IsSuccess)
            return expression.Error!;

        AdvanceTokenIf(Tokens.Semicolon);

        return new ReturnStatement { Token = returnToken, ReturnValue = expression.Value };
    }

    private Result<IAstStatement> ParseExpressionStatement()
    {
        var expression = ParseExpression(Precedence.Lowest);
        if (!expression.IsSuccess)
            return expression.Error!;

        var statement = new ExpressionStatement
        {
            Token = currentToken,
            Expression = expression.Value
        };

        AdvanceTokenIf(Tokens.Semicolon);

        return statement;
    }


    private Result<IExpression> ParseExpression(Precedence precedence = Precedence.Lowest)
    {
        Func<Result<IExpression>>? prefixFunction = currentToken.Type switch
        {
            Tokens.Identifier => this.ParseIdentifier,
            Tokens.Integer => this.ParseIntegerLiteral,
            Tokens.String => this.ParseStringLiteral,
            Tokens.Minus => this.ParsePrefixExpression,
            Tokens.Bang => this.ParsePrefixExpression,
            Tokens.True => this.ParseBooleanLiteral,
            Tokens.False => this.ParseBooleanLiteral,
            Tokens.LParen => this.ParseGroupedExpression,
            Tokens.If => this.ParseIfExpression,
            Tokens.Function => this.ParseFnExpression,
            _ => null,
        };

        if (prefixFunction == null)
            return new NotSupportedException($"No prefix parse function for token type {currentToken.Type}.");

        var leftExpression = prefixFunction();
        if (!leftExpression.IsSuccess)
            return leftExpression;

        while (PeekToken.Type != Tokens.Semicolon && precedence < OperatorPrecedence(PeekToken.Type))
        {
            Func<IExpression, Result<IExpression>>? infixFunction = PeekToken.Type switch
            {
                Tokens.Plus => this.ParseInfixExpression,
                Tokens.Minus => this.ParseInfixExpression,
                Tokens.Asterisk => this.ParseInfixExpression,
                Tokens.ForwardSlash => this.ParseInfixExpression,
                Tokens.GreaterThan => this.ParseInfixExpression,
                Tokens.LessThan => this.ParseInfixExpression,
                Tokens.Equal => this.ParseInfixExpression,
                Tokens.NotEqual => this.ParseInfixExpression,
                Tokens.LParen => this.ParseCallExpression,
                _ => null,
            };

            if (infixFunction == null)
                return leftExpression;

            AdvanceToken();
            leftExpression = infixFunction(leftExpression.Value);
        }

        return leftExpression;
    }

    private Result<IExpression> ParsePrefixExpression()
    {
        var operatorToken = currentToken;
        AdvanceToken();

        var right = ParseExpression(Precedence.Prefix);
        if (!right.IsSuccess)
            return right;

        return new PrefixExpression { Token = operatorToken, Operator = operatorToken.Literal, Right = right.Value };
    }

    private Result<IExpression> ParseInfixExpression(IExpression left)
    {
        var operatorToken = currentToken;
        var precedence = OperatorPrecedence(currentToken.Type);

        AdvanceToken();

        var right = ParseExpression(precedence);
        if (!right.IsSuccess)
            return right;

        return new InfixExpression { Token = operatorToken, Operator = operatorToken.Literal, Left = left, Right = right.Value };
    }

    private Result<IExpression> ParseCallExpression(IExpression function)
    {
        var callToken = currentToken;

        var arguments = ParseCallArguments();
        if (!arguments.IsSuccess)
            return arguments.Error!;

        return new CallExpression
        {
            Token = callToken,
            Function = function,
            Arguments = arguments.Value
        };
    }

    private Result<IExpression[]> ParseCallArguments()
    {
        if (AdvanceTokenIf(Tokens.RParen).IsSuccess)
            return Array.Empty<IExpression>();

        AdvanceToken();

        var argument = ParseExpression();
        if (!argument.IsSuccess)
            return argument.Error!;

        var arguments = new List<IExpression> { argument.Value };

        while (AdvanceTokenIf(Tokens.Comma).IsSuccess)
        {
            AdvanceToken();

            argument = ParseExpression();
            if (!argument.IsSuccess)
                return argument.Error!;

            arguments.Add(argument.Value);
        }

        var rparen = AdvanceTokenIf(Tokens.RParen);
        if (!rparen.IsSuccess)
            return rparen.Error!;

        return arguments.ToArray();
    }

    private Result<IExpression> ParseGroupedExpression()
    {
        AdvanceToken();
        var exp = ParseExpression();
        if (!exp.IsSuccess)
            return exp;

        var rparen = AdvanceTokenIf(Tokens.RParen);
        if (!rparen.IsSuccess)
            return rparen.Error!;

        return exp;
    }


    private Result<IExpression> ParseIfExpression()
    {
        var ifToken = currentToken;

        var lparen = AdvanceTokenIf(Tokens.LParen);
        if (!lparen.IsSuccess)
            return lparen.Error!;

        AdvanceToken();

        var condition = ParseExpression();
        if (!condition.IsSuccess)
            return condition.Error!;

        var rparen = AdvanceTokenIf(Tokens.RParen);
        if (!rparen.IsSuccess)
            return rparen.Error!;

        var lbrace = AdvanceTokenIf(Tokens.LSquirly);
        if (!lbrace.IsSuccess)
            return lbrace.Error!;

        var consequence = ParseBlockStatement();

        BlockStatement? alternative = null;

        var elseStatement = AdvanceTokenIf(Tokens.Else);
        if (elseStatement.IsSuccess)
        {
            lbrace = AdvanceTokenIf(Tokens.LSquirly);
            if (!lbrace.IsSuccess)
                return lbrace.Error!;

            var block = ParseBlockStatement();
            if (!block.IsSuccess)
                return block.Error!;

            alternative = block.Value;
        }

        return new IfExpression
        {
            Token = ifToken,
            Condition = condition.Value,
            Consequence = consequence.Value,
            Alternative = alternative
        };
    }

    private Result<IExpression> ParseFnExpression()
    {
        var fnToken = currentToken;

        var lparen = AdvanceTokenIf(Tokens.LParen);
        if (!lparen.IsSuccess)
            return lparen.Error!;

        var parameters = ParseFunctionParameters();
        if (!parameters.IsSuccess)
            return parameters.Error!;

        var lbrace = AdvanceTokenIf(Tokens.LSquirly);
        if (!lbrace.IsSuccess)
            return lbrace.Error!;

        var body = ParseBlockStatement();
        if (!body.IsSuccess)
            return body.Error!;

        return new FnExpression
        {
            Token = fnToken,
            Parameters = parameters.Value,
            Body = body.Value
        };
    }

    private Result<Identifier[]> ParseFunctionParameters()
    {
        var rparen = AdvanceTokenIf(Tokens.RParen);
        if (rparen.IsSuccess)
            return Array.Empty<Identifier>();

        AdvanceToken();

        var identifier = ParseIdentifier();
        if (!identifier.IsSuccess)
            return identifier.Error!;

        var identifiers = new List<Identifier> { (Identifier)identifier.Value };

        while (AdvanceTokenIf(Tokens.Comma).IsSuccess)
        {
            AdvanceToken();

            identifier = ParseIdentifier();
            if (!identifier.IsSuccess)
                return identifier.Error!;

            identifiers.Add((Identifier)identifier.Value);
        }

        rparen = AdvanceTokenIf(Tokens.RParen);
        if (!rparen.IsSuccess)
            return rparen.Error!;

        return identifiers.ToArray();
    }

    private Result<IExpression> ParseIdentifier()
    {
        return new Identifier { Token = currentToken, Value = currentToken.Literal };
    }

    private Result<IExpression> ParseIntegerLiteral()
    {
        if (!long.TryParse(currentToken.Literal, out long value))
            return new ArgumentOutOfRangeException($"Integer out of range {ExceptionLocatorString(currentToken)}");

        return new IntegerLiteral { Token = currentToken, Value = value };
    }

    private Result<IExpression> ParseStringLiteral()
    {
        if (!currentToken.Literal.EndsWith("\""))
            return new Exception($"String literal must be closed correctly {ExceptionLocatorString(currentToken)}");

        return new StringLiteral { Token = currentToken, Value = currentToken.Literal[1..(currentToken.Literal.Length - 1)] };
    }

    private Result<IExpression> ParseBooleanLiteral()
    {
        if (!bool.TryParse(currentToken.Literal, out bool value))
            return new ArgumentOutOfRangeException($"Boolean out of range {ExceptionLocatorString(currentToken)}");

        return new BooleanLiteral { Token = currentToken, Value = value };
    }

    private Result<BlockStatement> ParseBlockStatement()
    {
        var block = new BlockStatement
        {
            Token = currentToken,
            Statements = new List<IAstStatement>()
        };

        AdvanceToken();

        while (currentToken.Type != Tokens.RSquirly && currentToken.Type != Tokens.EndOfFile)
        {
            var statement = ParseStatement();
            if (!statement.IsSuccess)
                return statement.Error!;

            block.Statements.Add(statement.Value);
            AdvanceToken();
        }

        return block;
    }


    private Result<Token> AdvanceTokenIf(Tokens identifier)
    {
        if (PeekToken.Type != identifier)
            return new Exception($"Expected '{identifier}' but found '{PeekToken.Type}' {ExceptionLocatorString(PeekToken)}.");

        AdvanceToken();
        return currentToken;
    }

    private void AdvanceToken()
    {
        currentToken = tokenEnumerator.Current;
        tokenEnumerator.MoveNext();
    }

    private Token PeekToken => tokenEnumerator.Current;


    private static Precedence OperatorPrecedence(Tokens type) => type switch
    {
        Tokens.Equal => Precedence.Equals,
        Tokens.NotEqual => Precedence.Equals,
        Tokens.LessThan => Precedence.LessGreater,
        Tokens.GreaterThan => Precedence.LessGreater,
        Tokens.Plus => Precedence.Sum,
        Tokens.Minus => Precedence.Sum,
        Tokens.ForwardSlash => Precedence.Product,
        Tokens.Asterisk => Precedence.Product,
        Tokens.LParen => Precedence.Call,
        _ => Precedence.Lowest
    };

    private static string ExceptionLocatorString(Token token) => $"at Line {token.Line}, Columns {token.Column}";
}
