﻿using Microsoft.Extensions.Logging;
using MyCompiler.Entities;
using MyCompiler.Helpers;

namespace MyCompiler;

public class Parser
{
    private readonly IEnumerator<Token> tokenEnumerator;
    private readonly ILogger logger;
    private Token currentToken;

    public Parser(IEnumerable<Token> tokenSource, ILogger logger)
    {
        tokenEnumerator = tokenSource.GetEnumerator();
        this.logger = logger;
    }

    public Result<AstProgram> ParseProgram()
    {
        var program = new AstProgram();

        AdvanceToken();
        AdvanceToken();

        var allErrors = new List<Exception>();

        while (currentToken.Type != Tokens.EndOfFile)
        {

            Result<IAstStatement> statement = currentToken.Type switch
            {
                Tokens.Let => ParseLetStatement(),
                Tokens.Return => ParseReturnStatement(),
                //Tokens.If => ParseIfStatement(),
                Tokens.Semicolon => Result<IAstStatement>.Success(new EmptyStatement { Token = currentToken }),
                //_ => Result<IAstStatement>.Failure(new NotSupportedException($"Token type {currentToken.Type} is not yet supported.")) //Silently ignore other statements
                _ => ParseExpressionStatement()
            };

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
            ? Result<AstProgram>.Failure(new AggregateException(allErrors))
            : program;
    }

    private Result<IAstStatement> ParseLetStatement()
    {
        var letToken = currentToken;

        var identifierToken = AdvanceTokenIf(Tokens.Identifier);
        if (!identifierToken.IsSuccess)
            return Result<IAstStatement>.Failure(identifierToken.Error!);

        var identifier = new Identifier { Token = currentToken, Value = identifierToken.Value.Literal };

        var assignmentToken = AdvanceTokenIf(Tokens.Assign);
        if (!assignmentToken.IsSuccess)
            return Result<IAstStatement>.Failure(assignmentToken.Error!);


        //TODO: We're skipping the expressions until we encounter a semicolon.
        while (PeekToken.Type != Tokens.Semicolon)
            AdvanceToken();


        AdvanceTokenIf(Tokens.Semicolon);

        return new LetStatement { Token = letToken, Identifier = identifier };
    }

    private Result<IAstStatement> ParseReturnStatement()
    {
        var returnToken = currentToken;



        //TODO: We're skipping the expressions until we encounter a semicolon.
        while (PeekToken.Type != Tokens.Semicolon)
            AdvanceToken();
        var expression = new EmptyStatement();
        //var expression = ParseExpression();
        //if (!expression.IsSuccess)
        //    return Result<ReturnStatement>.Failure(expression.Error!);


        AdvanceTokenIf(Tokens.Semicolon);

        return new ReturnStatement { Token = returnToken, ReturnValue = expression };
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
            Tokens.Minus => this.ParsePrefixExpression,
            Tokens.Bang => this.ParsePrefixExpression,
            _ => null,
        };

        if (prefixFunction == null)
            return new NotSupportedException($"No prefix parse function for token type {currentToken.Type}.");

        var leftExpression = prefixFunction();
        if (!leftExpression.IsSuccess)
            return leftExpression;

        while (PeekToken.Type != Tokens.Semicolon && precedence < GetPrecedence(PeekToken.Type))
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
                _ => null,
            };

            if (infixFunction == null)
                return leftExpression;

            AdvanceToken();
            leftExpression = infixFunction(leftExpression.Value);
        }

        return leftExpression;
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
        var precedence = GetPrecedence(currentToken.Type);

        AdvanceToken();

        var right = ParseExpression(precedence);
        if (!right.IsSuccess)
            return right;

        return new InfixExpression { Token = operatorToken, Operator = operatorToken.Literal, Left = left, Right = right.Value };
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


    private static Precedence GetPrecedence(Tokens type) => type switch
    {
        Tokens.Equal => Precedence.Equals,
        Tokens.NotEqual => Precedence.Equals,
        Tokens.LessThan => Precedence.LessGreater,
        Tokens.GreaterThan => Precedence.LessGreater,
        Tokens.Plus => Precedence.Sum,
        Tokens.Minus => Precedence.Sum,
        Tokens.ForwardSlash => Precedence.Product,
        Tokens.Asterisk => Precedence.Product,
        _ => Precedence.Lowest
    };

    private static string ExceptionLocatorString(Token token) => $"at Line {token.Line}, Columns {token.Column}";
}
