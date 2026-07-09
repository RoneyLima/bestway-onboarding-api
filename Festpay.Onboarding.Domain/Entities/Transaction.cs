using Festpay.Onboarding.Domain.Exceptions;

namespace Festpay.Onboarding.Domain.Entities;

public class Transaction : EntityBase
{
    public Guid OriginAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public bool Canceled { get; private set; }

    public override void Validate()
    {
        if (Amount <= 0)
            throw new InvalidTransactionAmountException(Amount);

        if (OriginAccountId == DestinationAccountId)
            throw new SameAccountTransactionException(OriginAccountId, DestinationAccountId);
    }

    public void Cancel()
    {
        if (Canceled)
            throw new TransactionAlreadyCanceledException(Id);

        Canceled = true;
    }

    public class Builder
    {
        private readonly Transaction _transaction = new();

        public Builder WithOriginAccountId(Guid originAccountId)
        {
            _transaction.OriginAccountId = originAccountId;
            return this;
        }

        public Builder WithDestinationAccountId(Guid destinationAccountId)
        {
            _transaction.DestinationAccountId = destinationAccountId;
            return this;
        }

        public Builder WithAmount(decimal amount)
        {
            _transaction.Amount = amount;
            return this;
        }

        public Transaction Build()
        {
            _transaction.Validate();
            return _transaction;
        }
    }
}
