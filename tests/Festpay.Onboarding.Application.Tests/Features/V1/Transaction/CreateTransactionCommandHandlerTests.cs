using Festpay.Onboarding.Application.Common.Exceptions;
using Festpay.Onboarding.Application.Features.V1;
using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Domain.Exceptions;
using Festpay.Onboarding.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Tests.Features.V1.Transaction;

public class CreateTransactionCommandHandlerTests
{
    private readonly DbContextOptions<FestpayContext> _dbOptions = new DbContextOptionsBuilder<FestpayContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task Should_Create_Transaction_And_Update_Balances_When_Command_Is_Valid()
    {
        using var context = new FestpayContext(_dbOptions);
        var origin = CreateAccount("16670073607", 200m);
        var destination = CreateAccount("39053344705", 50m);
        context.Accounts.AddRange(origin, destination);
        await context.SaveChangesAsync();

        var handler = new CreateTransactionCommandHandler(context);

        var result = await handler.Handle(
            new CreateTransactionCommand(origin.Id, destination.Id, 75.25m),
            CancellationToken.None
        );

        var transaction = await context.Transactions.SingleAsync();

        Assert.True(result);
        Assert.Equal(origin.Id, transaction.OriginAccountId);
        Assert.Equal(destination.Id, transaction.DestinationAccountId);
        Assert.Equal(75.25m, transaction.Amount);
        Assert.False(transaction.Canceled);
        Assert.Equal(124.75m, origin.Balance);
        Assert.Equal(125.25m, destination.Balance);
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Origin_Account_Does_Not_Exist()
    {
        using var context = new FestpayContext(_dbOptions);
        var destination = CreateAccount("39053344705", 50m);
        context.Accounts.Add(destination);
        await context.SaveChangesAsync();

        var handler = new CreateTransactionCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateTransactionCommand(Guid.NewGuid(), destination.Id, 10m),
                CancellationToken.None
            )
        );

        Assert.Empty(context.Transactions);
        Assert.Equal(50m, destination.Balance);
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Destination_Account_Does_Not_Exist()
    {
        using var context = new FestpayContext(_dbOptions);
        var origin = CreateAccount("16670073607", 100m);
        context.Accounts.Add(origin);
        await context.SaveChangesAsync();

        var handler = new CreateTransactionCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateTransactionCommand(origin.Id, Guid.NewGuid(), 10m),
                CancellationToken.None
            )
        );

        Assert.Empty(context.Transactions);
        Assert.Equal(100m, origin.Balance);
    }

    [Fact]
    public async Task Should_Throw_ApplicationExceptions_When_Any_Account_Is_Inactive()
    {
        using var context = new FestpayContext(_dbOptions);
        var origin = CreateAccount("16670073607", 100m);
        var destination = CreateAccount("39053344705", 50m);
        destination.EnableDisable();
        context.Accounts.AddRange(origin, destination);
        await context.SaveChangesAsync();

        var handler = new CreateTransactionCommandHandler(context);

        await Assert.ThrowsAsync<ApplicationExceptions>(() =>
            handler.Handle(
                new CreateTransactionCommand(origin.Id, destination.Id, 10m),
                CancellationToken.None
            )
        );

        Assert.Empty(context.Transactions);
        Assert.Equal(100m, origin.Balance);
        Assert.Equal(50m, destination.Balance);
    }

    [Fact]
    public async Task Should_Throw_SameAccountTransactionException_When_Origin_And_Destination_Are_The_Same()
    {
        using var context = new FestpayContext(_dbOptions);
        var account = CreateAccount("16670073607", 100m);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var handler = new CreateTransactionCommandHandler(context);

        await Assert.ThrowsAsync<SameAccountTransactionException>(() =>
            handler.Handle(
                new CreateTransactionCommand(account.Id, account.Id, 10m),
                CancellationToken.None
            )
        );

        Assert.Empty(context.Transactions);
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public async Task Should_Throw_InsufficientBalanceException_When_Origin_Balance_Is_Insufficient()
    {
        using var context = new FestpayContext(_dbOptions);
        var origin = CreateAccount("16670073607", 25m);
        var destination = CreateAccount("39053344705", 10m);
        context.Accounts.AddRange(origin, destination);
        await context.SaveChangesAsync();

        var handler = new CreateTransactionCommandHandler(context);

        await Assert.ThrowsAsync<InsufficientBalanceException>(() =>
            handler.Handle(
                new CreateTransactionCommand(origin.Id, destination.Id, 30m),
                CancellationToken.None
            )
        );

        Assert.Empty(context.Transactions);
        Assert.Equal(25m, origin.Balance);
        Assert.Equal(10m, destination.Balance);
    }

    private static Account CreateAccount(string document, decimal balance)
    {
        return new Account.Builder()
            .WithName("Test Account")
            .WithEmail($"{document}@example.com")
            .WithPhone("11999999999")
            .WithDocument(document)
            .WithBalance(balance)
            .Build();
    }
}
