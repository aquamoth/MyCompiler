using Microsoft.Extensions.Logging;
using MyCompiler.Entities;
using MyCompiler.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

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

    public Result<ProgramNode> ParseProgram()
    {
        var program = new ProgramNode();

        AdvanceTokens();
        AdvanceTokens();

        while (currentToken.Type != Tokens.EndOfFile)
        {

            var statement = currentToken.Type switch
            {
                Tokens.Let => ParseLetStatement(),
                //Tokens.Return => ParseReturnStatement(),
                //Tokens.If => ParseIfStatement(),
                _ => Result<Node>.Failure(new NotSupportedException($"Token type {currentToken.Type} is not yet supported.")) //Silently ignore other statements
            };

            if (statement.IsSuccess)
            {
                program.Statements.Add(statement.Value);
            }
            else
            {
                logger?.LogCritical(statement.Error, "Error parsing source!");
                Console.WriteLine(statement.Error!.Message);
            }

            AdvanceTokens();
        }

        return program;
    }

    private Result<Node> ParseLetStatement()
    {
        var letToken = currentToken;

        var identifierToken = AdvanceTokensIf(Tokens.Identifier);
        if (!identifierToken.IsSuccess)
            return Result<Node>.Failure(identifierToken.Error!);

        var identifier = new IdentifierNode { Token = currentToken, Name = identifierToken.Value.Literal };

        var assignmentToken = AdvanceTokensIf(Tokens.Assign);
        if (!assignmentToken.IsSuccess)
            return Result<Node>.Failure(assignmentToken.Error!);


        //TODO: We're skipping the expressions until we encounter a semicolon.
        while (PeekToken.Type != Tokens.Semicolon)
            AdvanceTokens();


        return new LetStatement { Token = letToken, Identifier = identifier };
    }

    //private Result<IdentifierNode> ParseIdentifier()
    //{
    //    return new IdentifierNode { Token = currentToken };
    //}

    //private Result<Node> ParseExpression()
    //{
    //    if (currentToken.Type == Tokens.Integer)
    //    {
    //        if (PeekToken.Type == Tokens.Plus)
    //        {
    //            return ParseOperator();
    //        }
    //        else if (PeekToken.Type == Tokens.Semicolon)
    //        {
    //            return ParseIntegerLiteral();
    //        }
    //    }
    //    else if (currentToken.Type == Tokens.LParen)
    //    {
    //        return ParseGroupedExpression();
    //    }

    //    return Result<Node>.Failure(new NotImplementedException($"Not yet implemented token '{currentToken.Type}' at Line {currentToken.Line}, Columns {currentToken.Column}."));
    //}

    //private Result<Node> ParseOperatorExpression()
    //{
    //    var left = ParseIntegerLiteral();

    //    AdvanceTokens();
    //    var operatorToken = currentToken;

    //    AdvanceTokens();
    //    var right = ParseExpression();

    //    return new OperatorExpression
    //    {
    //        Left = left.Value,
    //        Operator = operatorToken,
    //        Right = right.Value
    //    };
    //}

    //private Result<Node> ParseIntegerLiteral()
    //{
    //    var token = currentToken;
    //    AdvanceTokens();

    //    if (token.Type != Tokens.Integer)
    //        return TokenFailure(Tokens.Integer);

    //    return Result<Node>.Success(new IdentifierNode { Token = token });
    //}

    private Result<Token> TokenFailure(Tokens expected, Token? token = null)
    {
        if (token == null) token = currentToken;

        return Result<Token>.Failure(
            new Exception(
                $"Expected '{expected}' but found '{token.Type}' at Line {token.Line}, Columns {token.Column}.")
            );
    }

    //private Result<Node> NodeFailure(Tokens expected)
    //{
    //    return Result<Node>.Failure(
    //        new ValidationException(
    //            $"Expected '{expected}' but found '{currentToken.Type}' at Line {currentToken.Line}, Columns {currentToken.Column}.")
    //        );
    //}

    private Result<Token> AdvanceTokensIf(Tokens identifier)
    {
        if (PeekToken.Type != identifier)
            return TokenFailure(identifier, PeekToken);

        AdvanceTokens();
        return currentToken;
    }

    private void AdvanceTokens()
    {
        currentToken = tokenEnumerator.Current;
        tokenEnumerator.MoveNext();
    }

    private Token PeekToken => tokenEnumerator.Current;
}
