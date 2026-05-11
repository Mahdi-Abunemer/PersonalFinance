using PersonalFinanceCli.Application.CommandHandlers;
using PersonalFinanceCli.Application.Services;
using PersonalFinanceCli.Domain.Services;
using PersonalFinanceCli.Infrastructure.Persistence;
using PersonalFinanceCli.Infrastructure.Time;
using PersonalFinanceCli.Presentation.Parsing;
using PersonalFinanceCli.Presentation.Rendering;

namespace PersonalFinanceCli;

public static class Program
{
    public static int Main(string[] args)
    {
        var console = new SystemConsole();
        var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data.json");

        var dataStore = new JsonDataStore(dataPath);
        var cardRepository = new JsonCardRepository(dataStore);
        var transactionRepository = new JsonTransactionRepository(dataStore);
        var limitRepository = new JsonLimitRepository(dataStore);
        var onboardingStateRepository = new JsonOnboardingStateRepository(dataStore);
        var clock = new SystemClock();

        var parser = new CommandParser();
        var addCardHandler = new AddCardHandler(cardRepository);
        var setDefaultCardHandler = new SetDefaultCardHandler(cardRepository);
        var addTransactionHandler = new AddTransactionHandler(
            transactionRepository,
            cardRepository,
            clock);
        var addIncomeHandler = new AddIncomeHandler(addTransactionHandler);
        var addExpenseHandler = new AddExpenseHandler(
            transactionRepository,
            cardRepository,
            clock);
        var cushionTransferService = new CushionTransferService(
            addTransactionHandler,
            transactionRepository,
            clock);
        var setDailyLimitHandler = new SetDailyLimitHandler(
            limitRepository,
            cardRepository,
            clock);
        var dailyReportService = new DailyReportService(
            cardRepository,
            transactionRepository,
            limitRepository);
        var cushionService = new CushionService(cardRepository);
        var reportPrinter = new ReportPrinter(
            console.Out,
            cardRepository,
            transactionRepository,
            limitRepository);
        var yesNoPrompt = new YesNoPrompt(console);
        var wizardPromptReader = new WizardPrompt(
            console,
            cardRepository);

        var consoleUi = new ConsoleUi(
            parser,
            addCardHandler,
            setDefaultCardHandler,
            addIncomeHandler,
            addExpenseHandler,
            setDailyLimitHandler,
            dailyReportService,
            reportPrinter,
            cardRepository,
            limitRepository,
            onboardingStateRepository,
            clock,
            console,
            cushionService,
            cushionTransferService,
            yesNoPrompt,
            wizardPromptReader);

        if (args.Length > 0)
        {
            return consoleUi.Execute(args);
        }

        consoleUi.RunInteractiveLoop();
        return 0;
    }
}
