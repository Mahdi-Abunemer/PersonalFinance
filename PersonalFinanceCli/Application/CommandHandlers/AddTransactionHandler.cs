using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Application.Validation;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddTransactionHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IClock _clock;

    public AddTransactionHandler(
        ITransactionRepository transactionRepository,
        ICardRepository cardRepository,
        IClock clock)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _clock = clock;
    }

    public Transaction Handle(
        TransactionType transactionType,
        decimal amount,
        string category,
        int? cardId,
        DateOnly? date,
        string? note)
    {
        TransactionValidator.ValidateAmount(amount);

        TransactionValidator.ValidateCategory(category);

        var selectedCardId = ResolveCardSelectedId(cardId, transactionType);
        var selectedCard = _cardRepository.GetById(selectedCardId);
        if (selectedCard is null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        var transaction = new Transaction 
        {
            CardId = selectedCardId, 
            Amount = amount, 
            Category = category, 
            Date = date ?? _clock.Today, 
            Note = note, 
            Type = transactionType 
        };

        return _transactionRepository.Add(transaction);
    }

    public int ResolveCardSelectedId(int? cardId, TransactionType transactionType)
    {
        if (cardId.HasValue)
        {
            var cardFoundById = _cardRepository.GetById(cardId.Value);
            if (cardFoundById == null)
            {
                throw new InvalidOperationException("Card not found.");
            }

            return cardFoundById.Id;
        }

        if (transactionType == TransactionType.Expense)
        {
            // Expense use the stored default card over the logical default card.
            var defaultCardByStore = _cardRepository.GetDefaultCardByDataStore();
            if (defaultCardByStore != null)
            {
                return defaultCardByStore.Id;
            }

            var firstExpenseCardInStore = _cardRepository.GetFirst();
            if (firstExpenseCardInStore != null)
            {
                return firstExpenseCardInStore.Id;
            }

            throw new InvalidOperationException("No cards available.");
        }

        var defaultCardByFlag = _cardRepository.GetDefault();
        // Income use the logical default card over the stored default card.
        if (defaultCardByFlag != null)
        {
            return defaultCardByFlag.Id;
        }

        var firstIncomeCardInStore = _cardRepository.GetFirst();
        if (firstIncomeCardInStore == null)
        {
            throw new InvalidOperationException("No cards available.");
        }

        return firstIncomeCardInStore.Id;
    }
}
