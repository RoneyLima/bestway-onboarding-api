using Festpay.Onboarding.Domain.Entities;
using Festpay.Onboarding.Domain.Exceptions;

namespace Festpay.Onboarding.Domain.Tests.Entities;

public class AccountTests
{
    [Fact]
    public void Should_Create_Account_When_Data_Is_Valid()
    {
        var account = new Account.Builder()
            .WithName("John Doe")
            .WithDocument("16670073607")
            .WithEmail("john.doe@example.com")
            .WithPhone("11999999999")
            .Build();

        Assert.Equal("John Doe", account.Name);
        Assert.Equal("16670073607", account.Document);
        Assert.Equal("john.doe@example.com", account.Email);
        Assert.Equal("11999999999", account.Phone);
    }

    [Fact]
    public void Should_Throw_RequiredFieldException_When_Name_Is_Empty()
    {
        var exception = Assert.Throws<RequiredFieldException>(
            () =>
                new Account.Builder()
                    .WithName("")
                    .WithDocument("16670073607")
                    .WithEmail("john.doe@example.com")
                    .WithPhone("11999999999")
                    .Build()
        );

        Assert.Equal("Name", exception.FieldName);
    }

    [Fact]
    public void Should_Throw_InvalidDocumentNumberException_When_Document_Is_Invalid()
    {
        var invalidDocument = "00000000000";

        var exception = Assert.Throws<InvalidDocumentNumberException>(
            () =>
                new Account.Builder()
                    .WithName("John Doe")
                    .WithDocument(invalidDocument)
                    .WithEmail("john.doe@example.com")
                    .WithPhone("11999999999")
                    .Build()
        );

        Assert.Equal(invalidDocument, exception.Document);
    }

    [Fact]
    public void Should_Throw_InvalidEmailFormatException_When_Email_Is_Invalid()
    {
        var invalidEmail = "john.doeexample.com";

        var exception = Assert.Throws<InvalidEmailFormatException>(
            () =>
                new Account.Builder()
                    .WithName("John Doe")
                    .WithDocument("16670073607")
                    .WithEmail(invalidEmail)
                    .WithPhone("11999999999")
                    .Build()
        );

        Assert.Equal(invalidEmail, exception.Email);
    }

    [Fact]
    public void Should_Throw_InvalidPhoneNumberException_When_Phone_Is_Invalid()
    {
        var invalidPhone = "123";

        var exception = Assert.Throws<InvalidPhoneNumberException>(
            () =>
                new Account.Builder()
                    .WithName("John Doe")
                    .WithDocument("16670073607")
                    .WithEmail("john.doe@example.com")
                    .WithPhone(invalidPhone)
                    .Build()
        );

        Assert.Equal(invalidPhone, exception.Phone);
    }

    [Fact]
    public void Should_Credit_Balance_When_Amount_Is_Valid()
    {
        var account = new Account.Builder()
            .WithName("John Doe")
            .WithDocument("16670073607")
            .WithEmail("john.doe@example.com")
            .WithPhone("11999999999")
            .WithBalance(100m)
            .Build();

        account.Credit(25.5m);

        Assert.Equal(125.5m, account.Balance);
    }

    [Fact]
    public void Should_Debit_Balance_When_Amount_Is_Available()
    {
        var account = new Account.Builder()
            .WithName("John Doe")
            .WithDocument("16670073607")
            .WithEmail("john.doe@example.com")
            .WithPhone("11999999999")
            .WithBalance(100m)
            .Build();

        account.Debit(40m);

        Assert.Equal(60m, account.Balance);
    }

    [Fact]
    public void Should_Throw_InsufficientBalanceException_When_Debit_Exceeds_Balance()
    {
        var account = new Account.Builder()
            .WithName("John Doe")
            .WithDocument("16670073607")
            .WithEmail("john.doe@example.com")
            .WithPhone("11999999999")
            .WithBalance(100m)
            .Build();

        var exception = Assert.Throws<InsufficientBalanceException>(() => account.Debit(100.01m));

        Assert.Equal(100m, exception.Balance);
        Assert.Equal(100.01m, exception.Amount);
        Assert.Equal(100m, account.Balance);
    }
}
