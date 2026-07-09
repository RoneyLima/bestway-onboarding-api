using Festpay.Onboarding.Application.Common.Exceptions;
using Festpay.Onboarding.Application.Features.V1;
using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Domain.Exceptions;
using Festpay.Onboarding.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Tests.Features.V1;

public class CancelTransactionCommandHandlerTests
{
    private readonly DbContextOptions<FestpayContext> _dbOptions = new DbContextOptionsBuilder<FestpayContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task Should_Cancel_Transaction_And_Revert_Balances_When_Transaction_Is_Active()
    {
        using var context = new FestpayContext(_dbOptions);
        var origin = CreateTestAccount("16670073607", 70m);
        var destination = CreateTestAccount("39053344705", 130m);
        var transaction = CreateTransaction(origin.Id, destination.Id, 30m);

        context.Accounts.AddRange(origin, destination);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var handler = new CancelTransactionCommandHandler(context);

        var result = await handler.Handle(
            new CancelTransactionCommand(transaction.Id),
            CancellationToken.None
        );

        Assert.True(result);
        Assert.True(transaction.Canceled);
        Assert.Equal(100m, origin.Balance);
        Assert.Equal(100m, destination.Balance);
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Transaction_Does_Not_Exist()
    {
        using var context = new FestpayContext(_dbOptions);
        var handler = new CancelTransactionCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelTransactionCommand(Guid.NewGuid()), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Should_Throw_TransactionAlreadyCanceledException_When_Transaction_Was_Already_Canceled()
    {
        using var context = new FestpayContext(_dbOptions);
        var origin = CreateTestAccount("16670073607", 100m);
        var destination = CreateTestAccount("39053344705", 100m);
        var transaction = CreateTransaction(origin.Id, destination.Id, 30m);
        transaction.Cancel();

        context.Accounts.AddRange(origin, destination);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var handler = new CancelTransactionCommandHandler(context);

        await Assert.ThrowsAsync<TransactionAlreadyCanceledException>(() =>
            handler.Handle(new CancelTransactionCommand(transaction.Id), CancellationToken.None)
        );

        Assert.Equal(100m, origin.Balance);
        Assert.Equal(100m, destination.Balance);
    }

    private static Account CreateTestAccount(string document, decimal balance)
    {
        return new Account.Builder()
            .WithName("Test Account")
            .WithEmail($"{document}@example.com")
            .WithPhone("11999999999")
            .WithDocument(document)
            .WithBalance(balance)
            .Build();
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
