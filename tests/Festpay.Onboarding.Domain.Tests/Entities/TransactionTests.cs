using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Domain.Exceptions;

namespace Festpay.Onboarding.Domain.Tests.Entities;

public class TransactionTests
{
    [Fact]
    public void Should_Create_Transaction_When_Data_Is_Valid()
    {
        var originAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();

        var transaction = new Transaction.Builder()
            .WithOriginAccountId(originAccountId)
            .WithDestinationAccountId(destinationAccountId)
            .WithAmount(150.75m)
            .Build();

        Assert.Equal(originAccountId, transaction.OriginAccountId);
        Assert.Equal(destinationAccountId, transaction.DestinationAccountId);
        Assert.Equal(150.75m, transaction.Amount);
        Assert.False(transaction.Canceled);
    }

    [Fact]
    public void Should_Throw_InvalidTransactionAmountException_When_Amount_Is_Zero()
    {
        var exception = Assert.Throws<InvalidTransactionAmountException>(
            () =>
                new Transaction.Builder()
                    .WithOriginAccountId(Guid.NewGuid())
                    .WithDestinationAccountId(Guid.NewGuid())
                    .WithAmount(0)
                    .Build()
        );

        Assert.Equal(0, exception.Amount);
    }

    [Fact]
    public void Should_Throw_InvalidTransactionAmountException_When_Amount_Is_Negative()
    {
        var exception = Assert.Throws<InvalidTransactionAmountException>(
            () =>
                new Transaction.Builder()
                    .WithOriginAccountId(Guid.NewGuid())
                    .WithDestinationAccountId(Guid.NewGuid())
                    .WithAmount(-10)
                    .Build()
        );

        Assert.Equal(-10, exception.Amount);
    }

    [Fact]
    public void Should_Throw_SameAccountTransactionException_When_Accounts_Are_Equal()
    {
        var accountId = Guid.NewGuid();

        var exception = Assert.Throws<SameAccountTransactionException>(
            () =>
                new Transaction.Builder()
                    .WithOriginAccountId(accountId)
                    .WithDestinationAccountId(accountId)
                    .WithAmount(50)
                    .Build()
        );

        Assert.Equal(accountId, exception.OriginAccountId);
        Assert.Equal(accountId, exception.DestinationAccountId);
    }

    [Fact]
    public void Should_Cancel_Transaction_When_It_Is_Active()
    {
        var transaction = new Transaction.Builder()
            .WithOriginAccountId(Guid.NewGuid())
            .WithDestinationAccountId(Guid.NewGuid())
            .WithAmount(50)
            .Build();

        transaction.Cancel();

        Assert.True(transaction.Canceled);
    }

    [Fact]
    public void Should_Throw_TransactionAlreadyCanceledException_When_Canceling_Twice()
    {
        var transaction = new Transaction.Builder()
            .WithOriginAccountId(Guid.NewGuid())
            .WithDestinationAccountId(Guid.NewGuid())
            .WithAmount(50)
            .Build();

        transaction.Cancel();

        var exception = Assert.Throws<TransactionAlreadyCanceledException>(transaction.Cancel);

        Assert.Equal(transaction.Id, exception.TransactionId);
    }
}
