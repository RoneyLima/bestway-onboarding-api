using Festpay.Onboarding.Application.Features.V1;
using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Tests.Features.V1.Transaction;

public class GetTransactionsQueryHandlerTests
{
    private readonly DbContextOptions<FestpayContext> _dbOptions = new DbContextOptionsBuilder<FestpayContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task Should_Return_Empty_Collection_When_There_Are_No_Transactions()
    {
        using var context = new FestpayContext(_dbOptions);
        var handler = new GetTransactionsQueryHandler(context);

        var result = await handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Should_Return_Active_And_Canceled_Transactions_With_Projection()
    {
        using var context = new FestpayContext(_dbOptions);
        var first = CreateTransaction(Guid.NewGuid(), Guid.NewGuid(), 10m);
        var second = CreateTransaction(Guid.NewGuid(), Guid.NewGuid(), 20.5m);
        second.Cancel();

        context.Transactions.AddRange(first, second);
        await context.SaveChangesAsync();

        var handler = new GetTransactionsQueryHandler(context);

        var result = await handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x =>
            x.Id == first.Id
            && x.OriginAccountId == first.OriginAccountId
            && x.DestinationAccountId == first.DestinationAccountId
            && x.Amount == 10m
            && x.Canceled == false
        );
        Assert.Contains(result, x =>
            x.Id == second.Id
            && x.OriginAccountId == second.OriginAccountId
            && x.DestinationAccountId == second.DestinationAccountId
            && x.Amount == 20.5m
            && x.Canceled
        );
    }

    private static Domain.Entities.Transaction CreateTransaction(Guid originId, Guid destinationId, decimal amount)
    {
        return new Domain.Entities.Transaction.Builder()
            .WithOriginAccountId(originId)
            .WithDestinationAccountId(destinationId)
            .WithAmount(amount)
            .Build();
    }
}
