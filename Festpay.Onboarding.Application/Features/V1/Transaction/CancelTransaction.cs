using Carter;
using Festpay.Onboarding.Application.Common.Constants;
using Festpay.Onboarding.Application.Common.Exceptions;
using Festpay.Onboarding.Application.Common.Models;
using Festpay.Onboarding.Infra.Context;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Features.V1;

public sealed record CancelTransactionCommand(Guid Id) : IRequest<bool>;

public sealed class CancelTransactionCommandHandler(FestpayContext dbContext)
    : IRequestHandler<CancelTransactionCommand, bool>
{
    public async Task<bool> Handle(
        CancelTransactionCommand request,
        CancellationToken cancellationToken
    )
    {
        var transaction =
            await dbContext.Transactions.FirstOrDefaultAsync(
                x => x.Id == request.Id,
                cancellationToken
            ) ?? throw new NotFoundException("Transação");

        var originAccount =
            await dbContext.Accounts.FirstOrDefaultAsync(
                x => x.Id == transaction.OriginAccountId,
                cancellationToken
            ) ?? throw new NotFoundException("Conta de origem");

        var destinationAccount =
            await dbContext.Accounts.FirstOrDefaultAsync(
                x => x.Id == transaction.DestinationAccountId,
                cancellationToken
            ) ?? throw new NotFoundException("Conta de destino");

        transaction.Cancel();
        destinationAccount.Debit(transaction.Amount);
        originAccount.Credit(transaction.Amount);

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}

public sealed class CancelTransactionCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch(
                $"{EndpointConstants.V1}{EndpointConstants.Transaction}/{{id:guid}}/cancel",
                async ([FromServices] ISender sender, [FromRoute] Guid id) =>
                {
                    var result = await sender.Send(new CancelTransactionCommand(id));
                    return Result.Ok(result);
                }
            )
            .WithTags(SwaggerTagsConstants.Transaction);
    }
}
