using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PersonalFinanceCli.Presentation.Exceptions;

namespace PersonalFinanceCli.Presentation.Rendering
{
    public sealed class WizardPrompt
    {
        private readonly IConsole _console;
        private readonly ICardRepository _cardRepository;

        public WizardPrompt(IConsole console, ICardRepository cardRepository)
        {
            _console = console;
            _cardRepository = cardRepository;
        }

        public string AskRequiredText(string? seed, string prompt)
        {
            var currentAnswer = seed;
            while (true)
            {
                currentAnswer = GetCurrentAnswer(prompt, currentAnswer);

                ThrowIfCancelled(currentAnswer);

                if (!string.IsNullOrWhiteSpace(currentAnswer))
                {
                    return currentAnswer;
                }

                _console.WriteLine("Error: Value is required.");
                currentAnswer = null;
            }
        }

        public decimal AskRequiredDecimal(string? seed, string prompt)
        {
            var currentAnswer = seed;
            while (true)
            {
                currentAnswer = GetCurrentAnswer(prompt, currentAnswer);

                ThrowIfCancelled(currentAnswer);

                if (TryParseFlexibleDecimal(currentAnswer, out var value))
                {
                    return value;
                }

                _console.WriteLine("Error: Invalid decimal.");
                currentAnswer = null;
            }
        }

        public decimal? AskOptionalDecimal(string? seed, string prompt)
        {
            var currentAnswer = seed;
            while (true)
            {
                currentAnswer = GetCurrentAnswer(prompt, currentAnswer);

                ThrowIfCancelled(currentAnswer);

                if (string.IsNullOrWhiteSpace(currentAnswer))
                {
                    return null;
                }

                if (TryParseFlexibleDecimal(currentAnswer, out var value))
                {
                    return value;
                }

                _console.WriteLine("Error: Invalid decimal.");
                currentAnswer = null;
            }
        }

        public DateOnly? AskOptionalDate(string? seed, string prompt)
        {
            var currentAnswer = seed;
            while (true)
            {
                currentAnswer = GetCurrentAnswer(prompt, currentAnswer);

                ThrowIfCancelled(currentAnswer);

                if (string.IsNullOrWhiteSpace(currentAnswer))
                {
                    return null;
                }

                if (DateOnly.TryParse(currentAnswer, out var value))
                {
                    return value;
                }

                _console.WriteLine("Error: Invalid date.");
                currentAnswer = null;
            }
        }

        public int? ResolveCardWizard(string? seed, string prompt)
        {
            var currentAnswer = seed;
            while (true)
            {
                currentAnswer = GetCurrentAnswer(prompt, currentAnswer);

                ThrowIfCancelled(currentAnswer);

                if (string.IsNullOrWhiteSpace(currentAnswer))
                {
                    return null;
                }

                if (int.TryParse(currentAnswer.Trim(), out var cardId))
                {
                    return cardId;
                }

                //Check if it's a guid (numbers: 0-9 , letters: a-f or A-F , dash: -)
                if (Regex.IsMatch(currentAnswer, "^[0-9a-fA-F-]{36}$")
                    && Guid.TryParse(currentAnswer, out var guid))
                {
                    var cardIdText = guid.ToString("N")[20..];
                    if (int.TryParse(cardIdText, out var cardIdFromGuid))
                    {
                        return cardIdFromGuid;
                    }
                }

                var cards = _cardRepository.GetAll();
                var cardByExact = cards
                    .FirstOrDefault(c => c.Name.Equals(
                        currentAnswer.Trim(),
                        StringComparison.OrdinalIgnoreCase));
                if (cardByExact != null)
                {
                    return cardByExact.Id;
                }

                var cardsByName = cards
                    .Where(c => c.Name.Contains(
                        currentAnswer,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (cardsByName.Count == 1)
                {
                    return cardsByName[0].Id;
                }

                _console.WriteLine("Error: Invalid card. Enter card id or card name.");
                currentAnswer = null;
            }
        }

        public string AskCurrency(string? seed)
        {
            var currentAnswer = seed;
            while (true)
            {
                if (currentAnswer == null)
                {
                    _console.Write("Currency (RUB/EUR)? ");
                    currentAnswer = ReadWizardAnswer();
                }

                ThrowIfCancelled(currentAnswer);

                if (Enum.TryParse<Currency>(currentAnswer, true, out _))
                {
                    return currentAnswer;
                }

                _console.WriteLine("Error: Unknown currency. Allowed: RUB, EUR.");
                currentAnswer = null;
            }
        }

        private string ReadWizardAnswer()
        {
            var answer = _console.ReadLine();
            if (answer == null)
            {
                throw new WizardCancelledException();
            }

            return answer;
        }

        private static bool TryParseFlexibleDecimal(string decimalText, out decimal value)
        {
            var normalizedDecimalText = decimalText.Trim().Replace(',', '.');
            return decimal.TryParse(
                normalizedDecimalText,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out value);
        }

        private string GetCurrentAnswer(string prompt, string? currentAnswer)
        {
            if (currentAnswer == null)
            {
                _console.Write($"{prompt} ");
                currentAnswer = ReadWizardAnswer();
            }

            return currentAnswer;
        }

        private static void ThrowIfCancelled(string? currentAnswer)
        {
            if (string.Equals(currentAnswer, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                throw new WizardCancelledException();
            }
        }

    }
}
