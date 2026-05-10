using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.Services;

public sealed class CushionService
{
    public const string ToCushionCategoryTransfer="Transfer to cushion";
    public const string FromIncomeCategoryTransfer="Transfer from income";
    private const string CushionCardName = "Financial cushion";

    private readonly ICardRepository _cardRepository;

    public CushionService(ICardRepository cardRepository)
    {
        _cardRepository=cardRepository;
    }

    public Card? FindCushionByName()
    {
        var cards = _cardRepository.GetAll();
        return cards.FirstOrDefault(c => c.Name == CushionCardName);
    }

    public Card? FindCushionByContains()
    {
        var cards = _cardRepository.GetAll();
        return cards.FirstOrDefault(
            c => c.Name
            .Contains("cushion", StringComparison.OrdinalIgnoreCase)
            );
    }

    public Card CreateCushion(Currency currency)
    {
        var card = FindCushionByName();
        if (card != null)
        {
            return card;
        }

        return _cardRepository.Add(
            new Card
            {
                Name = CushionCardName,
                Currency = currency,
                InitialBalance = 0m,
                IsDefault = false,
                IsCushion = true
            });
    }

    public decimal DefaultTransferAmount(decimal incomeAmount, string category)
    {
        var isCategorySalary = category.Contains("Salary", StringComparison.OrdinalIgnoreCase);

        if (incomeAmount < 10m)
        {
            return 1m;
        }
        else
        {
            if (isCategorySalary)
            {
                return Floor2(incomeAmount * 0.20m);
            }

            return Floor2(incomeAmount * 0.10m);
        }
    }

    public static decimal Floor2(decimal value)
    {
        return Math.Floor(value*100m)/100m;
    }

    public Card? FindFirstOrDefaultCushionCard()
    {
        var cards = _cardRepository.GetAll();
        var cardByCushionFlag = cards.FirstOrDefault(c => c.IsCushion);
        if (cardByCushionFlag != null)
        {
            return cardByCushionFlag;
        }

        var exactCushionCard = cards.FirstOrDefault(c => c.Name == CushionCardName);
        if (exactCushionCard != null)
        {
            return exactCushionCard;
        }

        return cards.FirstOrDefault(c => c.Name.Contains("cushion"));
    }
}
