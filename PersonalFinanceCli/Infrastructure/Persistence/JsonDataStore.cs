using System.Text.Json;
using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonDataStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;

    public JsonDataStore(string filePath)
    {
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public DataFile Load()
    {
        if (!File.Exists(_filePath))
        {
            var emptyFile = new DataFile();
            Save(emptyFile);
            return emptyFile;
        }

        var jsonText = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            var emptyFile = new DataFile();
            Save(emptyFile);
            return emptyFile;
        }

        // deserialize and then normalize collections because null is not list
        var dataFileResult = JsonSerializer.Deserialize<DataFile>(jsonText, _options);
        if (dataFileResult == null)
        {
            var emptyFile = new DataFile();
            Save(emptyFile);
            return emptyFile;
        }

        dataFileResult.Cards ??= new List<Card>();
        dataFileResult.Transactions ??= new List<Transaction>();
        dataFileResult.DailyLimits ??= new List<DailyLimit>();

        return dataFileResult;
    }

    public void Save(DataFile data)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var jsonText = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(_filePath, jsonText);
    }
}
