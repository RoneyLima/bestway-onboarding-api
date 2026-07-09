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

namespace Festpay.Onboarding.Application.Features.V1;

public sealed record ChargeAccountCommand(Guid Id, decimal Amount) : IRequest<bool>;

public sealed class ChargeAccountCommandValidator : AbstractValidator<ChargeAccountCommand>
{
    public ChargeAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}

public sealed class ChargeAccountCommandHandler(FestpayContext dbContext)
    : IRequestHandler<ChargeAccountCommand, bool>
{
    public async Task<bool> Handle(
        ChargeAccountCommand request,
        CancellationToken cancellationToken
    )
    {
        var account = await dbContext.Accounts.FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new NotFoundException("Conta");

        if (account.DeactivatedUtc.HasValue)
            throw new ApplicationExceptions("Contas inativas não podem receber cargas.");

        account.Credit(request.Amount);
        dbContext.Accounts.Update(account);

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}

public sealed record ChargeAccountRequest(decimal Amount);

internal sealed class ChargeAccountCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{EndpointConstants.V1}{EndpointConstants.Account}/{{id:guid}}/charge",
            async ([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] ChargeAccountRequest request) =>
            {
                var command = new ChargeAccountCommand(id, request.Amount);
                var result = await sender.Send(command);
                return Result.Ok(result);
            }
        )
        .WithTags(SwaggerTagsConstants.Account);
    }
}
