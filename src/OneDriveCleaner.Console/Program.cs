using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
			System.Console.WriteLine("Hello World!");
			var builder = new ConfigurationBuilder()
				.SetBasePath(System.AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json")
				.AddEnvironmentVariables()
				.AddUserSecrets<Program>();
			var configuration = builder.Build();

			var clientId = configuration["ClientId"];

			var scopes = new List<string> { "User.Read", "Files.Read.All" };
			var app = PublicClientApplicationBuilder.Create(clientId)
				.WithRedirectUri("http://localhost")
				.Build();
			var authenticationResult = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
			var accessToken = authenticationResult.AccessToken;

			var graphserviceClient = new GraphServiceClient(
				new DelegateAuthenticationProvider(
					(requestMessage) =>
					{
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
						return Task.FromResult(0);
					}));

			var driveRequest = graphserviceClient
				.Drive
				.Request();
			var driveResponse = await driveRequest.GetAsync();
		}
	}
}
