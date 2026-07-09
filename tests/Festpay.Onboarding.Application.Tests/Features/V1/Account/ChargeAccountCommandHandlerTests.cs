using Festpay.Onboarding.Application.Common.Exceptions;
using Festpay.Onboarding.Application.Features.V1;
using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Festpay.Onboarding.Application.Tests.Features.V1;

public class ChargeAccountCommandHandlerTests
{
    private readonly DbContextOptions<FestpayContext> _dbOptions;

    public ChargeAccountCommandHandlerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<FestpayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static Account CreateTestAccount(decimal balance = 0m)
    {
        return new Account.Builder()
            .WithName("Test Account")
            .WithEmail("test@example.com")
            .WithPhone("11999999999")
            .WithDocument("12345678909")
            .WithBalance(balance)
            .Build();
    }

    [Fact]
    public async Task Should_Credit_Account_When_Command_Is_Valid()
    {
        var account = CreateTestAccount(100m);

        using var context = new FestpayContext(_dbOptions);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var handler = new ChargeAccountCommandHandler(context);

        var result = await handler.Handle(
            new ChargeAccountCommand(account.Id, 25.5m),
            CancellationToken.None
        );

        var updatedAccount = await context.Accounts.FindAsync(account.Id);

        Assert.True(result);
        Assert.NotNull(updatedAccount);
        Assert.Equal(125.5m, updatedAccount!.Balance);
    }

    [Fact]
    public async Task Should_Throw_NotFoundException_When_Account_Does_Not_Exist()
    {
        using var context = new FestpayContext(_dbOptions);
        var handler = new ChargeAccountCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ChargeAccountCommand(Guid.NewGuid(), 10m), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Should_Throw_ApplicationExceptions_When_Account_Is_Inactive()
    {
        var account = CreateTestAccount(100m);
        account.EnableDisable();

        using var context = new FestpayContext(_dbOptions);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var handler = new ChargeAccountCommandHandler(context);

        await Assert.ThrowsAsync<ApplicationExceptions>(() =>
            handler.Handle(new ChargeAccountCommand(account.Id, 10m), CancellationToken.None)
        );

        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void Should_Invalidate_Command_When_Id_Is_Empty()
    {
        var validator = new ChargeAccountCommandValidator();

        var result = validator.Validate(new ChargeAccountCommand(Guid.Empty, 10m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Id is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Should_Invalidate_Command_When_Amount_Is_Not_Greater_Than_Zero(decimal amount)
    {
        var validator = new ChargeAccountCommandValidator();

        var result = validator.Validate(new ChargeAccountCommand(Guid.NewGuid(), amount));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage == "Amount must be greater than zero."
        );
    }
}
