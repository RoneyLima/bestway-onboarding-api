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

public sealed record GetTransactionByIdQuery(Guid Id) : IRequest<GetTransactionsQueryResponse>;

public sealed class GetTransactionByIdQueryHandler(FestpayContext dbContext)
    : IRequestHandler<GetTransactionByIdQuery, GetTransactionsQueryResponse>
{
    public async Task<GetTransactionsQueryResponse> Handle(
        GetTransactionByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var transaction =
            await dbContext.Transactions.FirstOrDefaultAsync(
                x => x.Id == request.Id,
                cancellationToken
            ) ?? throw new NotFoundException("Transaction");

        return new GetTransactionsQueryResponse(
            transaction.Id,
            transaction.OriginAccountId,
            transaction.DestinationAccountId,
            transaction.Amount,
            transaction.Canceled,
            transaction.CreatedUtc,
            transaction.DeactivatedUtc
        );
    }
}

public sealed class GetTransactionByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                $"{EndpointConstants.V1}{EndpointConstants.Transaction}/{{id:guid}}",
                async ([FromServices] ISender sender, [FromRoute] Guid id) =>
                {
                    var result = await sender.Send(new GetTransactionByIdQuery(id));
                    return Result.Ok(result);
                }
            )
            .WithTags(SwaggerTagsConstants.Transaction);
    }
}
