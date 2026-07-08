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

public sealed record ChangeAccountStatusCommand(Guid Id) : IRequest<bool>;

public sealed class ChangeAccountStatusCommandValidator
    : AbstractValidator<ChangeAccountStatusCommand>
{
    public ChangeAccountStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
    }
}

public sealed class ChangeAccountStatusCommandHandler(FestpayContext dbContext)
    : IRequestHandler<ChangeAccountStatusCommand, bool>
{
    public async Task<bool> Handle(
        ChangeAccountStatusCommand request,
        CancellationToken cancellationToken
    )
    {
        // TODO: DESCOMENTAR CÓDIGO E REMOVER O ACCOUNT MOCKADO
        var account =
            await dbContext.Accounts.FindAsync(request.Id)
            ?? throw new NotFoundException("Conta");

        // var account = new Account.Builder()
        //     .WithName("Teste")
        //     .WithDocument("12345678901")
        //     .WithEmail("joao@gmail.com")
        //     .WithPhone("11999999999")
        //     .Build();

        account.EnableDisable();
        dbContext.Accounts.Update(account);

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}

public sealed class ChangeAccountStatusCommandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch($"{EndpointConstants.V1}{EndpointConstants.Account}/{{id:guid}}",
            async ([FromServices] ISender sender, [FromRoute] Guid id) =>
            {
                var command = new ChangeAccountStatusCommand(id);
                var result = await sender.Send(command);
                return Result.Ok(result);
            }
        )
        .WithTags(SwaggerTagsConstants.Account);
    }
}
