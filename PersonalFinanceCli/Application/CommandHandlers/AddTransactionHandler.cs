using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddTransactionHandler
{
    public const string TransferToCushion = "Transfer to cushion";
    public const string TransferFromIncome = "Transfer from income";

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
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

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

    public int ResolveCardId(int? cardId)
    {
        return ResolveCardSelectedId(cardId, TransactionType.Income);
    }

    public Card? FindFirstOrDefaultCushionCard()
    {
        var cards = _cardRepository.GetAll();
        var cardByCushionFlag = cards.FirstOrDefault(c => c.IsCushion);
        if (cardByCushionFlag != null)
        {
            return cardByCushionFlag;
        }

        var exactCushionCard = cards.FirstOrDefault(c => c.Name == "Financial cushion");
        if (exactCushionCard != null)
        {
            return exactCushionCard;
        }

        return cards.FirstOrDefault(c => c.Name.Contains("cushion"));
    }

    public void AddTransferPair(int fromCardId, int cushionCardId, decimal amount, DateOnly? date)
    {
        var transferDate = date ?? _clock.Today;

        _transactionRepository.Add(new Transaction
        {
            CardId = fromCardId,
            Amount = amount,
            Category = TransferToCushion,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Expense
        });

        _transactionRepository.Add(new Transaction
        {
            CardId = cushionCardId,
            Amount = amount,
            Category = TransferFromIncome,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Income
        });
    }
}
