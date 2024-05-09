using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OneDriveTool;

IServiceProvider serviceProvider;
ILogger<Program> logger;

var builder = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddEnvironmentVariables()
	.AddUserSecrets<Program>();
var configuration = builder.Build();

var exportOption = new Option<bool>("--export") { Description = "Export OneDrive metadata" };
exportOption.AddAlias("-e");

var analyzeOption = new Option<bool>("--analyze") { Description = "Analyze OneDrive export file" };
analyzeOption.AddAlias("-a");

var scanOption = new Option<string>("--scan") { Description = "Scan local folder recursively" };
scanOption.AddAlias("-s");

var oneDriveFileOption = new Option<string>("--onedrive-file") { Description = "OneDrive CSV file" };
oneDriveFileOption.AddAlias("-f");

var scanFileOption = new Option<string>("--scan-file") { Description = "Scan result output file" };
scanFileOption.AddAlias("-sf");

var loggingOption = new Option<string>(
	"--logging",
	"Logging verbosity")
		.FromAmong(
			"trace",
			"debug",
			"info");
loggingOption.SetDefaultValue("info");

int returnCode = 0;
var rootFolder = Directory.GetCurrentDirectory();

var rootCommand = new RootCommand(@"
  ___             ____       _              _              _
 / _ \ _ __   ___|  _ \ _ __(_)_   _____   | |_ ___   ___ | |
| | | | '_ \ / _ \ | | | '__| \ \ / / _ \  | __/ _ \ / _ \| |
| |_| | | | |  __/ |_| | |  | |\ V /  __/  | || (_) | (_) | |
\___ /|_| |_|\___|____/|_|  |_| \_/ \___|   \__\___/ \___/|_|

More information can be found here:
https://github.com/JanneMattila/onedrive-tool")
{
	exportOption,
	analyzeOption,
	scanOption,
	oneDriveFileOption,
	scanFileOption,
	loggingOption
};

rootCommand.SetHandler(async (export, analyze, scan, file, scanFile, logging) =>
{
	var loggingLevel = logging switch
	{
		"trace" => LogLevel.Trace,
		"debug" => LogLevel.Debug,
		"info" => LogLevel.Information,
		_ => LogLevel.Information
	};

	var services = new ServiceCollection();
	services.AddLogging(builder => {
		builder.SetMinimumLevel(loggingLevel);
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, CustomConsoleFormatter>());
		builder.AddConsole(options =>
		{
			options.MaxQueueLength = 1;
			options.FormatterName = "custom";
		});
	});
	services.AddSingleton<OneDriveManager>();
	serviceProvider = services.BuildServiceProvider();

	using var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
	ArgumentNullException.ThrowIfNull(loggerFactory);

	logger = loggerFactory.CreateLogger<Program>();

	var fileExists = !string.IsNullOrEmpty(file) && File.Exists(file);
	var manager = serviceProvider.GetRequiredService<OneDriveManager>();

	if (export)
	{
		logger.LogInformation("Run Export...");
		var clientId = configuration["ClientId"];
		manager.AuthenticateUsingClient(clientId);

		await manager.ExportAsync(file);
	}
	else if (analyze)
	{
		logger.LogInformation("Run Analyze...");
		if (!fileExists)
		{
			logger.LogError("File '{File}' does not exist.", file);
			return;
		}
		manager.Analyze(file);
	}
	else if (!string.IsNullOrEmpty(scan))
	{
		var path = Path.GetFullPath(scan);
		if (!Directory.Exists(path))
		{
			logger.LogError("Folder '{Path}' does not exist.", path);
			return;
		}

		logger.LogInformation("Run Scan...");
		manager.Scan(file, path, scanFile);
	}
	else
	{
		Console.WriteLine("Required arguments missing.");
		Console.WriteLine("Try '--help' for more information.");
	}
}, exportOption, analyzeOption, scanOption, oneDriveFileOption, scanFileOption, loggingOption);

await rootCommand.InvokeAsync(args);

return returnCode;
