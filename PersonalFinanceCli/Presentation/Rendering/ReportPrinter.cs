using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.Services;
using PersonalFinanceCli.Domain.ValueObjects;
using System.Globalization;

namespace PersonalFinanceCli.Presentation.Rendering;

public sealed class ReportPrinter
{
    private readonly TextWriter _writer;
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILimitRepository _limitRepository;

    public ReportPrinter(
        TextWriter writer,
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ILimitRepository limitRepository)
    {
        _writer = writer;
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _limitRepository = limitRepository;
    }

    public void Print(DailyReport report)
    {
        _writer.WriteLine($"Date: {report.Date:yyyy-MM-dd}");
        _writer.WriteLine($"Income: {FormatMoney(report.Income, report.Currency)}");
        _writer.WriteLine($"Expense: {FormatMoney(report.Expense, report.Currency)}");
        PrintLimitWithFloorPercent(
            report.Expense,
            report.Limit?.Amount,
            report.Limit?.Currency ?? report.Currency);

        var recalculatedCategories = RecalculateCategories(report.Date, report.Currency);
        _writer.WriteLine("By category:");
        foreach (var pair in recalculatedCategories.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            _writer.WriteLine($"  {pair.Key}: {FormatMoney(pair.Value, report.Currency)}");
        }

        _writer.WriteLine("Cards:");
        foreach (var card in report.Cards.OrderBy(c => c.CardId))
        {
            var marker = card.IsDefault ? " (default)" : string.Empty;
            _writer.WriteLine(
                $"  {card.CardName}{marker}: " +
                $"{FormatMoney(card.Balance, card.Currency)}");
        }
    }

    public void PrintDayUsingRepositories(DateOnly date)
    {
        var cards = _cardRepository.GetAll();
        var currency = GetReportCurrency(cards);
        var cardIds = GetCardIdsByCurrency(cards, currency);

        var allTransactions = _transactionRepository.GetAll();

        decimal income = 0m;
        decimal expense = 0m;
        var categoryTotals = new Dictionary<string, decimal>();

        foreach (var transaction in allTransactions)
        {
            if (transaction.Date == date && cardIds.Contains(transaction.CardId))
            {
                if (transaction.Type == TransactionType.Income)
                {
                    income += transaction.Amount;
                }
                else
                {
                    expense += transaction.Amount;
                    AddCategoryTotal(categoryTotals, transaction);
                }
            }
        }

        var limit = _limitRepository.GetByDate(date);

        _writer.WriteLine($"Date: {date:yyyy-MM-dd}");
        _writer.WriteLine($"Income: {income:F2} {currency}");
        _writer.WriteLine($"Expense: {expense:F2} {currency}");
        PrintLimitWithRoundPercent(expense, limit?.Amount, limit?.Currency ?? currency);

        _writer.WriteLine("By category:");
        foreach (var pair in categoryTotals.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            _writer.WriteLine($"  {pair.Key}: {pair.Value:F2} {currency}");
        }

        PrintCards(cards, allTransactions);
    }

    private void PrintCards(IReadOnlyList<Card> cards,
        IReadOnlyList<Transaction> allTransactions)
    {
        _writer.WriteLine("Cards:");
        foreach (var card in cards.OrderBy(c => c.Id))
        {
            decimal balance = CalculateCardBalance(allTransactions, card);

            var defaultSuffix = card.IsDefault ? " (default)" : "";
            _writer.WriteLine($"  {card.Name}{defaultSuffix}: {balance:F2} {card.Currency}");
        }
    }

    private static decimal CalculateCardBalance(IReadOnlyList<Transaction> allTransactions, Card card)
    {
        decimal balance = card.InitialBalance;
        foreach (var transaction in allTransactions)
        {
            if (transaction.CardId == card.Id)
            {
                balance = transaction.Type == TransactionType.Income
                    ? balance + transaction.Amount
                    : balance - transaction.Amount;
            }
        }

        return balance;
    }

    private static HashSet<int> GetCardIdsByCurrency(
        IReadOnlyList<Card> cards,
        Currency currency)
    {
        return cards
            .Where(c => c.Currency == currency)
            .Select(c => c.Id)
            .ToHashSet();
    }

    private static Currency GetReportCurrency(IReadOnlyList<Card> cards)
    {
        return cards.FirstOrDefault(c => c.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? Currency.RUB;
    }

    private static void AddCategoryTotal(
        Dictionary<string, decimal> categoryTotals,
        Transaction transaction)
    {
        if (categoryTotals.TryGetValue(transaction.Category, out var previousAmount))
        {
            categoryTotals[transaction.Category] = previousAmount + transaction.Amount;
        }
        else
        {
            categoryTotals[transaction.Category] = transaction.Amount;
        }
    }

    private void PrintLimitWithFloorPercent(decimal expense, decimal? limit, Currency currency)
    {
        if (TryPrintMissingLimit(limit))
        {
            return;
        }

        var percent = (int)Math.Floor((expense / limit!.Value) * 100m);
            _writer.WriteLine($"Limit: {FormatMoney(limit.Value, currency)} ({percent}%)");
    }

    private void PrintLimitWithRoundPercent(decimal expense, decimal? limit, Currency currency)
    {
        if (TryPrintMissingLimit(limit))
        {
            return;
        }

        var percent = limit!.Value == 0m 
                ? 0 
                : (int)Math.Round((expense / limit.Value) * 100m, MidpointRounding.AwayFromZero);
            _writer.WriteLine($"Limit: {limit.Value:F2} {currency} ({percent}%)");
    }

    private Dictionary<string, decimal> RecalculateCategories(DateOnly date, Currency currency)
    {
        var cards = _cardRepository.GetAll();
        var cardIds = GetCardIdsByCurrency(cards, currency);

        var categoryTotals = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var transaction in _transactionRepository.GetAll())
        {
            if (transaction.Date != date 
                || transaction.Type != TransactionType.Expense 
                || !cardIds.Contains(transaction.CardId))
            {
                continue;
            }

            AddCategoryTotal(categoryTotals, transaction);
        }

        return categoryTotals;
    }

    private static string FormatMoney(decimal amount, Currency currency)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:F2} {currency}");
    }

    private bool TryPrintMissingLimit(decimal? limit)
    {
        if (!limit.HasValue || limit.Value <= 0)
        {
            _writer.WriteLine("Limit: (not set)");
            return true;
        }

        return false;
    }
}
