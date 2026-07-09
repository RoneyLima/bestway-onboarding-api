using Carter;
using Festpay.Onboarding.Application.Common.Constants;
using Festpay.Onboarding.Application.Common.Exceptions;
using Festpay.Onboarding.Application.Common.Models;
using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Infra.Context;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Features.V1;

public sealed record CreateTransactionCommand(
    Guid OriginAccountId,
    Guid DestinationAccountId,
    decimal Amount
) : IRequest<bool>;

public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.OriginAccountId).NotEmpty().WithMessage("OriginAccountId is required.");
        RuleFor(x => x.DestinationAccountId).NotEmpty().WithMessage("DestinationAccountId is required.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}

public sealed class CreateTransactionCommandHandler(FestpayContext dbContext)
    : IRequestHandler<CreateTransactionCommand, bool>
{
    public async Task<bool> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken
    )
    {
        var originAccount =
            await dbContext.Accounts.FirstOrDefaultAsync(
                x => x.Id == request.OriginAccountId,
                cancellationToken
            ) ?? throw new NotFoundException("Origin account");

        var destinationAccount =
            await dbContext.Accounts.FirstOrDefaultAsync(
                x => x.Id == request.DestinationAccountId,
                cancellationToken
            ) ?? throw new NotFoundException("Destiny account");

        if (originAccount.DeactivatedUtc.HasValue || destinationAccount.DeactivatedUtc.HasValue)
            throw new ApplicationExceptions("Inative account can not realize transactions.");

        var duplicateThreshold = DateTime.UtcNow.AddMinutes(-5);
        var isDuplicate = await dbContext.Transactions.AnyAsync(
            x => x.OriginAccountId == request.OriginAccountId &&
                 x.DestinationAccountId == request.DestinationAccountId &&
                 x.Amount == request.Amount &&
                 !x.Canceled &&
                 x.CreatedUtc >= duplicateThreshold,
            cancellationToken
        );

        if (isDuplicate)
            throw new ApplicationExceptions("Duplicated transaction detected. Wait some minutes and try again if you're sure.");

        var transaction = new Transaction.Builder()
            .WithOriginAccountId(request.OriginAccountId)
            .WithDestinationAccountId(request.DestinationAccountId)
            .WithAmount(request.Amount)
            .Build();

        originAccount.Debit(request.Amount);
        destinationAccount.Credit(request.Amount);

        await dbContext.Transactions.AddAsync(transaction, cancellationToken);

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}

internal sealed class CreateTransactionCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                $"{EndpointConstants.V1}{EndpointConstants.Transaction}",
                async ([FromServices] ISender sender, [FromBody] CreateTransactionCommand command) =>
                {
                    var result = await sender.Send(command);
                    return Result.Ok(result);
                }
            )
            .WithTags(SwaggerTagsConstants.Transaction);
    }
}
