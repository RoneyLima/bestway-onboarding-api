using Carter;
using Festpay.Onboarding.Application.Common.Constants;
using Festpay.Onboarding.Application.Common.Models;
using Festpay.Onboarding.Infra.Context;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Features.V1;

public sealed record GetAccountsQueryResponse(
    Guid Id,
    string Name,
    string Document,
    string Email,
    string Phone,
    DateTime CreatedAt,
    DateTime? DeactivatedAt,
    decimal Balance
);

public sealed record GetAccountsQuery : IRequest<ICollection<GetAccountsQueryResponse>>;

public sealed class GetAccountsQueryHandler(FestpayContext dbContext) : IRequestHandler<GetAccountsQuery, ICollection<GetAccountsQueryResponse>>
{
    public async Task<ICollection<GetAccountsQueryResponse>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await dbContext.Accounts.ToListAsync(cancellationToken);

        return accounts
            .Select(a => new GetAccountsQueryResponse(
                a.Id,
                a.Name,
                a.Document,
                a.Email,
                a.Phone,
                a.CreatedUtc,
                a.DeactivatedUtc,
                a.Balance
            ))
            .ToList();
    }
}

public sealed class GetAccountsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet($"{EndpointConstants.V1}{EndpointConstants.Account}",
            async ([FromServices] ISender sender) =>
            {
                var result = await sender.Send(new GetAccountsQuery());
                return Result.Ok(result);
            }
        )
        .WithTags(SwaggerTagsConstants.Account);
    }
}
