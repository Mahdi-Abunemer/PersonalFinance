using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddExpenseHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IClock _clock;

    public AddExpenseHandler(
        ITransactionRepository transactionRepository,
        ICardRepository cardRepository,
        IClock clock)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _clock = clock;
    }

    public Transaction Handle(
        decimal amount,
        string category,
        int? cardId,
        DateOnly? date,
        string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        int resolvedCardId;
        if (cardId.HasValue)
        {
            var cardFoundById = _cardRepository.GetById(cardId.Value);
            if (cardFoundById == null)
            {
                throw new InvalidOperationException("Card not found.");
            }

            resolvedCardId = cardFoundById.Id;
        }
        else
        {
            var defaultCardByStore = _cardRepository.GetDefaultByDataStore();
            if (defaultCardByStore != null)
            {
                resolvedCardId = defaultCardByStore.Id;
            }
            else
            {
                var firstCardInStore = _cardRepository.GetFirst();
                if (firstCardInStore == null)
                {
                    throw new InvalidOperationException("No cards available.");
                }

                resolvedCardId = firstCardInStore.Id;
            }
        }

        var transaction = new Transaction
        {
            CardId = resolvedCardId,
            Amount = amount,
            Category = category,
            Date = date ?? _clock.Today,
            Note = note,
            Type = TransactionType.Expense
        };

        return _transactionRepository.Add(transaction);
    }
}
