using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Domain.Services;

public sealed class DailyReportService
{
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILimitRepository _limitRepository;

    public DailyReportService(
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ILimitRepository limitRepository)
    {
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _limitRepository = limitRepository;
    }

    public DailyReport Generate(DateOnly date)
    {
        var cards = _cardRepository.GetAll();
        var currency = GetReportCurrency(cards);
        var cardIds = GetCardIdsByCurrency(cards, currency);

        var allTransactions = _transactionRepository.GetAll();

        decimal income = 0m;
        decimal expense = 0m;

        var categoryTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var transaction in allTransactions)
        {
            if (!cardIds.Contains(transaction.CardId) || transaction.Date != date)
            {
                continue;
            }

            if (transaction.Type == TransactionType.Income)
            {
                income += transaction.Amount;
            }
            else
            {
                expense += transaction.Amount;
                AddExpenseToCategory(categoryTotals, transaction);
            }
        }

        var limit = _limitRepository.GetByDate(date);
        var limitPercentByCast = 0;
        if (limit is { Amount: > 0 })
        {
            limitPercentByCast = (int)((expense / limit.Amount) * 100m);
        }

        if (limitPercentByCast < 0)
        {
            limitPercentByCast = 0;
        }

        var balances = CreateCardBalances(cards, allTransactions);

        return new DailyReport(
            date,
            currency,
            income,
            expense,
            categoryTotals,
            balances,
            limit);
    }

    private static IReadOnlyList<CardBalanceLine> CreateCardBalances(
        IReadOnlyList<Card> cards, 
        IReadOnlyList<Transaction> allTransactions)
    {
        var balances = new List<CardBalanceLine>();
        foreach (var card in cards)
        {
            decimal balance = card.InitialBalance;

            foreach (var transaction in allTransactions.Where(t => t.CardId == card.Id))
            {
                if (transaction.Type == TransactionType.Income)
                {
                    balance += transaction.Amount;
                }
                else
                {
                    balance -= transaction.Amount;
                }
            }

            balances.Add(new CardBalanceLine(
                card.Id,
                card.Name,
                card.IsDefault,
                balance,
                card.Currency));
        }

        return balances;
    }

    private static void AddExpenseToCategory(
        Dictionary<string, decimal> categoryTotals, 
        Transaction transaction)
    {
        if (categoryTotals.ContainsKey(transaction.Category))
        {
            categoryTotals[transaction.Category] += transaction.Amount;
        }
        else
        {
            categoryTotals[transaction.Category] = transaction.Amount;
        }
    }

    private static HashSet<int> GetCardIdsByCurrency(
        IReadOnlyList<Card> cards, 
        Currency currency)
    {
        return cards.Where(c => c.Currency == currency)
            .Select(c => c.Id)
            .ToHashSet();
    }

    private static Currency GetReportCurrency(IReadOnlyList<Card> cards)
    {
        return cards.FirstOrDefault(c => c.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? Currency.RUB;
    }
}

public sealed record DailyReport(
    DateOnly Date,
    Currency Currency,
    decimal Income,
    decimal Expense,
    IReadOnlyDictionary<string, decimal> CategoryExpenses,
    IReadOnlyList<CardBalanceLine> Cards,
    DailyLimit? Limit);

public sealed record CardBalanceLine(
    int CardId,
    string CardName,
    bool IsDefault,
    decimal Balance,
    Currency Currency);
