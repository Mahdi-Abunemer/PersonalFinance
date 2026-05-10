using PersonalFinanceCli.Application.CommandHandlers;
using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;


namespace PersonalFinanceCli.Application.Services
{
    public class CushionTransferService
    {
        public const string TransferToCushion = "Transfer to cushion";
        public const string TransferFromIncome = "Transfer from income";

        private readonly AddTransactionHandler _addTransactionHandler;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IClock _clock;

        public CushionTransferService(
            AddTransactionHandler addTransactionHandler, 
            ITransactionRepository transactionRepository,
            IClock clock)
        {
            _addTransactionHandler = addTransactionHandler;
            _transactionRepository = transactionRepository;
            _clock = clock;
        }

        public void AddTransferPair(
        int fromCardId,
        int cushionCardId,
        decimal amount,
        DateOnly? date)
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

        public int ResolveCardId(int? cardId)
        {
            return _addTransactionHandler.ResolveCardSelectedId(cardId, TransactionType.Income);
        }
    }
}