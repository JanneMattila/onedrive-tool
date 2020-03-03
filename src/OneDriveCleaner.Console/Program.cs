using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace OneDriveCleaner.Console
{
	class Program
	{
		public static IConfigurationRoot Configuration { get; set; }

		static async Task Main(string[] args)
		{
			System.Console.WriteLine(@"  ___             ____       _");
			System.Console.WriteLine(@" / _ \ _ __   ___|  _ \ _ __(_)_   _____");
			System.Console.WriteLine(@"| | | | '_ \ / _ \ | | | '__| \ \ / / _ \");
			System.Console.WriteLine(@"| |_| | | | |  __/ |_| | |  | |\ V /  __/");
			System.Console.WriteLine(@"\___ /|_| |_|\___|____/|_|  |_| \_/ \___|");
			System.Console.WriteLine(@"  ____ _");
			System.Console.WriteLine(@" / ___| | ___  __ _ _ __   ___ _ __");
			System.Console.WriteLine(@"| |   | |/ _ \/ _` | '_ \ / _ \ '__|");
			System.Console.WriteLine(@"| |___| |  __/ (_| | | | |  __/ |");
			System.Console.WriteLine(@" \____|_|\___|\__,_|_| |_|\___|_|");
			System.Console.WriteLine();
			System.Console.WriteLine();
			if (args.Length == 0)
			{
				PrintUsage();
				return;
			}
			var builder = new ConfigurationBuilder()
				.SetBasePath(System.AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json")
				.AddEnvironmentVariables()
				.AddUserSecrets<Program>();
			var configuration = builder.Build();

			var manager = new OneDriveManager();
			if (args[0] == "export")
			{
				var clientId = configuration["ClientId"];
				await manager.AuthenticateAsync(clientId);
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
		}

		private static void PrintUsage()
		{
			System.Console.WriteLine("Usage:");
			System.Console.WriteLine("	export		Exports to CSV file");
			System.Console.WriteLine("	analyze		Analyzes the CSV file");
			System.Console.WriteLine();
		}
	}
}
