using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace OneDriveCleaner.Console
{
	class Program
	{
		public static IConfigurationRoot Configuration { get; set; }

		static async Task Main(string[] args)
		{
			System.Console.WriteLine("OneDrive Cleaner");
			var builder = new ConfigurationBuilder()
				.SetBasePath(System.AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json")
				.AddEnvironmentVariables()
				.AddUserSecrets<Program>();
			var configuration = builder.Build();

			var clientId = configuration["ClientId"];

			var manager = new OneDriveManager();
			await manager.AuthenticateAsync(clientId);
			await manager.ScanAsync();
		}
	}
}
