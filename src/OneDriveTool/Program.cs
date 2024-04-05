using Microsoft.Extensions.Configuration;
using OneDriveTool;

Console.WriteLine(@"  ___             ____       _              _              _ ");
Console.WriteLine(@" / _ \ _ __   ___|  _ \ _ __(_)_   _____   | |_ ___   ___ | |");
Console.WriteLine(@"| | | | '_ \ / _ \ | | | '__| \ \ / / _ \  | __/ _ \ / _ \| |");
Console.WriteLine(@"| |_| | | | |  __/ |_| | |  | |\ V /  __/  | || (_) | (_) | |");
Console.WriteLine(@"\___ /|_| |_|\___|____/|_|  |_| \_/ \___|   \__\___/ \___/|_|");

if (args.Length == 0)
{
	PrintUsage();
	return;
}
var builder = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddEnvironmentVariables()
	.AddUserSecrets<Program>();
var configuration = builder.Build();

var manager = new OneDriveManager();
if (args[0] == "export")
{
	var clientId = configuration["ClientId"];
	manager.AuthenticateUsingClient(clientId);
	await manager.ScanAsync();
}
else if (args[0] == "analyze")
{
	manager.Analyze();
}
else
{
	PrintUsage();
}


static void PrintUsage()
{
	Console.WriteLine("Usage:");
	Console.WriteLine("	export		Exports to CSV file");
	Console.WriteLine("	analyze		Analyzes the CSV file");
	Console.WriteLine();
}
