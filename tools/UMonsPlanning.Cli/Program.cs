using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UMonsPlanning.Pronote;

// Small command-line verification utility.
//
//   dotnet run --project tools/UMonsPlanning.Cli -- formations
//   dotnet run --project tools/UMonsPlanning.Cli -- sections ".BAB3 - Traduction et interprétation"
//   dotnet run --project tools/UMonsPlanning.Cli -- schedule bab3-traduction-et-interpretation 3
//   dotnet run --project tools/UMonsPlanning.Cli -- calendar

using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b
    .AddSimpleConsole(o => o.SingleLine = true)
    .SetMinimumLevel(LogLevel.Information));

var options = Options.Create(new PronoteOptions());
using var client = new PronoteClient(options, loggerFactory.CreateLogger<PronoteClient>());

var json = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

string command = args.Length > 0 ? args[0].ToLowerInvariant() : "formations";

switch (command)
{
    case "formations":
        Print(await client.GetFormationsAsync());
        break;

    case "sections":
        Require(args.Length >= 2, "usage: sections <formation>");
        Print(await client.GetSectionsAsync(args[1]));
        break;

    case "schedule":
        Require(args.Length >= 3, "usage: schedule <formation> <week> [section]");
        Print(await client.GetScheduleAsync(args[1], args.Length > 3 ? args[3] : null, int.Parse(args[2])));
        break;

    case "calendar":
        Print(await client.GetCalendarAsync());
        break;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        return 1;
}

return 0;

void Print<T>(T value) => Console.WriteLine(JsonSerializer.Serialize(value, json));

void Require(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine(message);
        Environment.Exit(2);
    }
}
