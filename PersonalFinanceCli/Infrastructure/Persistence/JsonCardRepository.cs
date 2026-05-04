using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonCardRepository : ICardRepository
{
    private readonly JsonDataStore _store;

    public JsonCardRepository(JsonDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<Card> GetAll()
    {
        return _store.Load().Cards.OrderBy(c => c.Id).ToList();
    }

    public Card? GetById(int id)
    {
        return _store.Load().Cards.FirstOrDefault(c => c.Id == id);
    }

    public Card? GetDefault()
    {
        return _store.Load().Cards.FirstOrDefault(c => c.IsDefault);
    }

    public Card? GetDefaultCardByDataStore()
    {
        var dataStore = _store.Load();
        if (!dataStore.DefaultCardId.HasValue)
        {
            return null;
        }

        var id = GuidToCardId(dataStore.DefaultCardId.Value);
        return dataStore.Cards.FirstOrDefault(c => c.Id == id);
    }

    public Card? GetFirst()
    {
        return _store.Load().Cards.OrderBy(c => c.Id).FirstOrDefault();
    }

    public Card Add(Card card)
    {
        var dataStore = _store.Load();

        card.Id = dataStore.Cards.Count == 0 ? 1 : dataStore.Cards.Max(c => c.Id) + 1;
        if (dataStore.Cards.Count == 0)
        {
            card.IsDefault = true;
            dataStore.DefaultCardId = CardIdToGuid(card.Id);
        }

        dataStore.Cards.Add(card);
        _store.Save(dataStore);
        return card;
    }

    public void SetDefault(int cardId)
    {
        var dataStore = _store.Load();
        foreach (var card in dataStore.Cards)
        {
            card.IsDefault = card.Id == cardId;
        }

        dataStore.DefaultCardId = CardIdToGuid(cardId);

        _store.Save(dataStore);
    }

    private static Guid CardIdToGuid(int cardId)
    {
        var cardIdString = cardId.ToString("D12");
        return Guid.Parse($"00000000-0000-0000-0000-{cardIdString}");
    }

    private static int GuidToCardId(Guid guid)
    {
        var guidString = guid.ToString("N");
        var cardIdPart = guidString.Substring(guidString.Length - 12, 12);
        return int.TryParse(cardIdPart, out var result) ? result : -1;
    }
}
