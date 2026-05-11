using PersonalFinanceCli.Application.CommandHandlers;
using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Application.Services;
using PersonalFinanceCli.Domain.Services;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;
using PersonalFinanceCli.Presentation.Parsing;
using System.Globalization;
using System.Text.RegularExpressions;
using PersonalFinanceCli.Presentation.Exceptions;

namespace PersonalFinanceCli.Presentation.Rendering;

public sealed class ConsoleUi
{
    private readonly CommandParser _parser;
    private readonly AddCardHandler _addCardHandler;
    private readonly SetDefaultCardHandler _setDefaultCardHandler;
    private readonly AddIncomeHandler _addIncomeHandler;
    private readonly AddExpenseHandler _addExpenseHandler;
    private readonly SetDailyLimitHandler _setDailyLimitHandler;
    private readonly DailyReportService _dailyReportService;
    private readonly ReportPrinter _reportPrinter;
    private readonly ICardRepository _cardRepository;
    private readonly ILimitRepository _limitRepository;
    private readonly IOnboardingStateRepository _onboardingStateRepository;
    private readonly IClock _clock;
    private readonly IConsole _console;
    private readonly CushionService _cushionService;
    private readonly WizardOptionCollector _wizardOptionCollector;
    private bool _isOnboardingChecked;
    private readonly CushionTransferService _cushionTransferService;
    private readonly YesNoPrompt _yesNoPrompt;
    private readonly WizardPrompt _wizardPromptReader;

    public ConsoleUi(
        CommandParser parser,
        AddCardHandler addCardHandler,
        SetDefaultCardHandler setDefaultCardHandler,
        AddIncomeHandler addIncomeHandler,
        AddExpenseHandler addExpenseHandler,
        SetDailyLimitHandler setDailyLimitHandler,
        DailyReportService dailyReportService,
        ReportPrinter reportPrinter,
        ICardRepository cardRepository,
        ILimitRepository limitRepository,
        IOnboardingStateRepository onboardingStateRepository,
        IClock clock,
        IConsole console,
        CushionService cushionService,
        CushionTransferService cushionTransferService,
        YesNoPrompt yesNoPrompt,
        WizardPrompt wizardPromptReader)
    {
        _parser = parser;
        _addCardHandler = addCardHandler;
        _setDefaultCardHandler = setDefaultCardHandler;
        _addIncomeHandler = addIncomeHandler;
        _addExpenseHandler = addExpenseHandler;
        _setDailyLimitHandler = setDailyLimitHandler;
        _dailyReportService = dailyReportService;
        _reportPrinter = reportPrinter;
        _cardRepository = cardRepository;
        _limitRepository = limitRepository;
        _onboardingStateRepository = onboardingStateRepository;
        _clock = clock;
        _console = console;
        _cushionService = cushionService;
        _wizardOptionCollector = new WizardOptionCollector();
        _cushionTransferService = cushionTransferService;
        _yesNoPrompt = yesNoPrompt;
        _wizardPromptReader = wizardPromptReader;
    }

    public int Execute(string[] args)
    {
        try
        {
            var parsedCommand = _parser.Parse(args);
            ExecuteParsedCommand(parsedCommand);
            return 0;
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    public void RunInteractiveLoop()
    {
        EnsureOnboardingOnce();

        while (true)
        {
            _console.Write("> ");
            var line = _console.ReadLine();
            if (line is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (line.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                continue;
            }

            if (TryHandleWizard(line))
            {
                continue;
            }

            try
            {
                var parsedCommand = _parser.Parse(line);
                ExecuteParsedCommand(parsedCommand);
            }
            catch (Exception ex)
            {
                _console.WriteLine($"Error: {ex.Message}");
                _console.WriteLine("type help");
            }
        }
    }

    private void EnsureOnboardingOnce()
    {
        if (_isOnboardingChecked)
        {
            return;
        }

        _isOnboardingChecked = true;

        var hasSeenOnboarding = _onboardingStateRepository.HasSeenOnboarding();

        var cushionCard = _cushionService.FindCushionByName()
            ?? _cushionService.FindFirstOrDefaultCushionCard()
            ?? _cushionService.FindCushionByContains();
        if (cushionCard != null)
        {
            _onboardingStateRepository.SetLastCushionDeclinedDate(null);
            _onboardingStateRepository.SetHasSeenOnboarding(true);
            return;
        }

        var cards = _cardRepository.GetAll();
        if (hasSeenOnboarding && cards.Count == 0)
        {
            return;
        }

        var lastDeclined = _onboardingStateRepository.GetLastCushionDeclinedDate();
        if (lastDeclined.HasValue && _clock.Today < lastDeclined.Value.AddDays(14))
        {
            return;
        }

        if (_yesNoPrompt.AskYesNoDefaultNo("Create 'Financial cushion' account? (y/n)"))
        {
            _cushionService.CreateCushion(Currency.RUB);
            _onboardingStateRepository.SetLastCushionDeclinedDate(null);
        }
        else
        {
            _onboardingStateRepository.SetLastCushionDeclinedDate(_clock.Today);
        }

        _onboardingStateRepository.SetHasSeenOnboarding(true);
    }

    private bool TryHandleWizard(string line)
    {
        var tokens = Tokenizer.Tokenize(line);
        if (tokens.Count < 2)
        {
            return false;
        }

        var commandName = tokens[0].ToLowerInvariant();
        var action = tokens[1].ToLowerInvariant();

        if (commandName == "card" && action == "add")
        {
            HandleCardAddWizard(tokens);
            return true;
        }

        if (commandName == "expense" && action == "add")
        {
            HandleExpenseAddWizard(tokens);
            return true;
        }

        if (commandName == "income" && action == "add")
        {
            HandleIncomeAddWizard(tokens);
            return true;
        }

        if (commandName == "limit" && action == "set")
        {
            HandleLimitSetWizard(tokens);
            return true;
        }

        return false;
    }

    private void HandleCardAddWizard(IReadOnlyList<string> tokens)
    {
        try
        {
            var cardName = _wizardPromptReader
                .AskRequiredText(tokens.Count >= 3 ? tokens[2] : null, "Card name?");

            var currency = _wizardPromptReader.AskCurrency(
                tokens.Count >= 4 
                ? tokens[3] 
                : null);

            var initialBalance = _wizardPromptReader.AskOptionalDecimal(
                tokens.Count >= 5 ? tokens[4] : null,
                "Initial balance? (enter = 0)");
            ExecuteParsedCommand(new CardAddCommand(cardName, currency, initialBalance));
        }
        catch (WizardCancelledException)
        {
            _console.WriteLine("Cancelled.");
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Error: {ex.Message}");
            _console.WriteLine("type help");
        }
    }

    private void HandleExpenseAddWizard(IReadOnlyList<string> tokens)
    {
        try
        {
            var amount = _wizardPromptReader
                .AskRequiredDecimal(tokens.Count >= 3 ? tokens[2] : null, "Amount?");

            var categoryToken = tokens.Count >= 4 ? tokens[3] : null;
            var optionsIndex = 4;
            if (categoryToken != null
                && categoryToken.StartsWith("--", StringComparison.Ordinal))
            {
                categoryToken = null;
                optionsIndex = 3;
            }

            var options = _wizardOptionCollector.Collect(tokens, optionsIndex);
            if (options.Error != null)
            {
                _console.WriteLine($"Error: {options.Error}");
                _console.WriteLine("type help");
                return;
            }

            var category = _wizardPromptReader.AskRequiredText(categoryToken, "Category?");
            var cardId = _wizardPromptReader.ResolveCardWizard(
                options.CardRaw, 
                "Card? (enter to use default, id or name)");
            var date = options.Date ?? _wizardPromptReader.AskOptionalDate(
                null,
                "Date? (YYYY-MM-DD, enter = today)");

            _addExpenseHandler.Handle(amount, category, cardId, date, options.Note);

            var dailyReport = _dailyReportService.Generate(_clock.Today);
            _reportPrinter.Print(dailyReport);
        }
        catch (WizardCancelledException)
        {
            _console.WriteLine("Cancelled.");
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Error: {ex.Message}");
            _console.WriteLine("type help");
        }
    }

    private void HandleIncomeAddWizard(IReadOnlyList<string> tokens)
    {
        try
        {
            var amount = _wizardPromptReader.
                AskRequiredDecimal(tokens.Count >= 3 ? tokens[2] : null, "Amount?");

            var categoryToken = tokens.Count >= 4 ? tokens[3] : null;
            var optionsIndex = 4;
            if (categoryToken != null
                && categoryToken.StartsWith("--", StringComparison.Ordinal))
            {
                categoryToken = null;
                optionsIndex = 3;
            }

            var options = _wizardOptionCollector.Collect(tokens, optionsIndex);
            if (options.Error != null)
            {
                _console.WriteLine($"Error: {options.Error}");
                _console.WriteLine("type help");
                return;
            }

            var category = _wizardPromptReader.AskRequiredText(categoryToken, "Category?");
            var cardId = _wizardPromptReader.ResolveCardWizard(
                options.CardRaw,
                "Card? (enter to use default, id or name)");
            var date = options.Date ?? _wizardPromptReader.AskOptionalDate(
                null,
                "Date? (YYYY-MM-DD, enter = today)");

            var sourceCardId = _cushionTransferService.ResolveCardId(cardId);
            _addIncomeHandler.Handle(amount, category, sourceCardId, date, options.Note);

            HandleOptionalCushionTransfer(amount, category, sourceCardId, date);

            var dailyReport = _dailyReportService.Generate(_clock.Today);
            _reportPrinter.Print(dailyReport);
        }
        catch (WizardCancelledException)
        {
            _console.WriteLine("Cancelled.");
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Error: {ex.Message}");
            _console.WriteLine("type help");
        }
    }

    private void HandleLimitSetWizard(IReadOnlyList<string> tokens)
    {
        try
        {
            var amount = _wizardPromptReader.AskRequiredDecimal(
                tokens.Count >= 3 ? tokens[2] : null,
                "Daily limit amount?");
            ExecuteParsedCommand(new LimitSetCommand(amount));
        }
        catch (WizardCancelledException)
        {
            _console.WriteLine("Cancelled.");
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Error: {ex.Message}");
            _console.WriteLine("type help");
        }
    }

    private void HandleOptionalCushionTransfer(
        decimal incomeAmount,
        string category,
        int sourceCardId,
        DateOnly? date)
    {
        if (!_yesNoPrompt.AskYesNoDefaultYes(
            "Transfer part of income to 'Financial cushion'? (y/n)"))
        {
            return;
        }

        var sourceCard = _cardRepository.GetById(sourceCardId);
        if (sourceCard == null)
        {
            return;
        }

        var cushionCard = _cushionService.FindCushionByName()
            ?? _cushionService.FindFirstOrDefaultCushionCard()
            ?? _cushionService.FindCushionByContains();

        if (cushionCard == null)
        {
            if (_yesNoPrompt.AskYesNo("Cushion account not found. Create now? (y/n)"))
            {
                cushionCard = _cushionService.CreateCushion(sourceCard.Currency);
            }
            else
            {
                return;
            }
        }

        if (sourceCard.Currency != cushionCard.Currency)
        {
            var hasCanceledMismatch = false;
            if (!_yesNoPrompt.AskYesNoWithCancel(
                "Currencies do not match. Transfer anyway? (y/n)",
                out hasCanceledMismatch))
            {
                return;
            }
        }

        if (category == "transfer to cushion ")
        {
            _console.WriteLine("Debug category branch reached.");
        }

        var transferAmount = AskTransferAmount(incomeAmount, category);
        if (!transferAmount.HasValue)
        {
            _console.WriteLine("Transfer cancelled.");
            return;
        }

        _cushionTransferService.AddTransferPair(
            sourceCardId,
            cushionCard.Id,
            transferAmount.Value,
            date);
    }

    private decimal? AskTransferAmount(decimal incomeAmount, string category)
    {
        while (true)
        {
            _console.Write("How much to transfer? " +
                "(enter = default / percent like 25% or absolute amount) ");

            var transferAmountInput = _console.ReadLine();
            if (transferAmountInput == null
                || transferAmountInput.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            decimal amount;
            if (string.IsNullOrWhiteSpace(transferAmountInput))
            {
                amount = _cushionService.DefaultTransferAmount(incomeAmount, category);
            }
            else if (transferAmountInput.TrimEnd().EndsWith("%", StringComparison.Ordinal))
            {
                var precentInputRaw = transferAmountInput.Trim()[..^1];
                if (!decimal.TryParse(precentInputRaw, out var percent))
                {
                    _console.WriteLine("Error: Invalid transfer amount.");
                    continue;
                }

                amount = CushionService.Floor2(incomeAmount * percent / 100m);
            }
            else
            {
                if (!decimal.TryParse(transferAmountInput.Trim(), out var explicitAmount))
                {
                    _console.WriteLine("Error: Invalid transfer amount.");
                    continue;
                }

                amount = Math.Round(explicitAmount, 2, MidpointRounding.AwayFromZero);
            }

            if (amount <= 0m || amount > incomeAmount)
            {
                _console.WriteLine($"Error: Transfer amount must be > 0 and <= income " +
                    $"({UiMoneyFormatter.FormatMoneyShort(incomeAmount)} max).");
                continue;
            }

            return amount;
        }
    }

    private void ExecuteParsedCommand(ParsedCommand parsedCommand)
    {
        var isStateChanged = false;

        switch (parsedCommand)
        {
            case CardAddCommand add:
                _addCardHandler.Handle(add.Name, add.Currency, add.InitialBalance);
                isStateChanged = true;
                break;
            case CardListCommand:
                PrintCards();
                break;
            case CardSetDefaultCommand setDefault:
                _setDefaultCardHandler.Handle(setDefault.CardId);
                isStateChanged = true;
                break;
            case TransactionAddCommand transaction:
                if (transaction.Type == TransactionType.Income)
                {
                    _addIncomeHandler.Handle(
                        transaction.Amount,
                        transaction.Category,
                        transaction.CardId,
                        transaction.Date,
                        transaction.Note);
                }
                else
                {
                    _addExpenseHandler.Handle(
                        transaction.Amount,
                        transaction.Category,
                        transaction.CardId,
                        transaction.Date,
                        transaction.Note);
                }

                isStateChanged = true;
                break;
            case LimitSetCommand setLimit:
                _setDailyLimitHandler.Handle(setLimit.Amount);
                isStateChanged = true;
                break;
            case LimitShowCommand:
                ShowLimit();
                break;
            case ReportDayCommand report:
                _reportPrinter.PrintDayUsingRepositories(report.Date ?? _clock.Today);
                break;
            default:
                throw new InvalidOperationException("Unknown parsed command.");
        }

        if (isStateChanged)
        {
            var dailyReport = _dailyReportService.Generate(_clock.Today);
            _reportPrinter.Print(dailyReport);
        }
    }

    private void PrintHelp()
    {
        _console.WriteLine("Commands:");
        _console.WriteLine("  help");
        _console.WriteLine("  exit");
        _console.WriteLine("  card add \"Name\" <RUB|EUR> [initialBalance]");
        _console.WriteLine("  card list");
        _console.WriteLine("  card set-default <cardId>");
        _console.WriteLine("  expense add <amount> <category> " +
            "[--card <id>] [--date YYYY-MM-DD] [--note \"text\"]");
        _console.WriteLine("  income add <amount> <category> " +
            "[--card <id>] [--date YYYY-MM-DD] [--note \"text\"]");
        _console.WriteLine("  limit set <amount>");
        _console.WriteLine("  limit show");
        _console.WriteLine("  report day [--date YYYY-MM-DD]");
    }

    private void PrintCards()
    {
        var cards = _cardRepository.GetAll();
        if (cards.Count == 0)
        {
            _console.WriteLine("Cards: (empty)");
            return;
        }

        _console.WriteLine("Cards:");
        foreach (var card in cards)
        {
            var marker = card.IsDefault ? " (default)" : string.Empty;
            _console.WriteLine($"  {card.Id}: {card.Name}{marker} " +
                $"[{card.Currency}] {card.InitialBalance:F2}");
        }
    }

    private void ShowLimit()
    {
        var today = _clock.Today;
        var limit = _limitRepository.GetByDate(today);
        if (limit is null)
        {
            _console.WriteLine("Limit: (not set)");
            return;
        }

        var cards = _cardRepository.GetAll();
        var currency = cards.FirstOrDefault(c => c.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? limit.Currency;

        _console.WriteLine($"Limit: {limit.Amount:F2} {currency} ({today:yyyy-MM-dd})");
    }
}
