using Festpay.Onboarding.Application.Common.Exceptions;
using Festpay.Onboarding.Application.Features.V1;
using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Tests.Features.V1.Transaction;

public class GetTransactionByIdQueryHandlerTests
{
    private readonly DbContextOptions<FestpayContext> _dbOptions = new DbContextOptionsBuilder<FestpayContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task Should_Return_Transaction_With_Complete_Projection_When_Id_Exists()
    {
        using var context = new FestpayContext(_dbOptions);
        var transaction = CreateTransaction(Guid.NewGuid(), Guid.NewGuid(), 45.75m);
        transaction.Cancel();
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var handler = new GetTransactionByIdQueryHandler(context);

        var result = await handler.Handle(
            new GetTransactionByIdQuery(transaction.Id),
            CancellationToken.None
        );

        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(transaction.OriginAccountId, result.OriginAccountId);
        Assert.Equal(transaction.DestinationAccountId, result.DestinationAccountId);
        Assert.Equal(45.75m, result.Amount);
        Assert.True(result.Canceled);
        Assert.Equal(transaction.CreatedUtc, result.CreatedUtc);
        Assert.Equal(transaction.DeactivatedUtc, result.DeactivatedUtc);
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Transaction_Id_Does_Not_Exist()
    {
        using var context = new FestpayContext(_dbOptions);
        var handler = new GetTransactionByIdQueryHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTransactionByIdQuery(Guid.NewGuid()), CancellationToken.None)
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
