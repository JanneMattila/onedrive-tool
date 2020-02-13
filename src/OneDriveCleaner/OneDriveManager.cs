using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace OneDriveCleaner
{
	public class OneDriveManager
    {
		private string _accessToken;

		public OneDriveManager()
		{
		}

		public async Task AuthenticateAsync(string clientId)
		{
			var scopes = new List<string> { "User.Read", "Files.Read.All" };
			var app = PublicClientApplicationBuilder.Create(clientId)
				.WithRedirectUri("http://localhost")
				.Build();
			var authenticationResult = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
			_accessToken = authenticationResult.AccessToken;
		}

		public async Task ScanAsync()
		{
			var graphserviceClient = new GraphServiceClient(
				new DelegateAuthenticationProvider(
					(requestMessage) =>
					{
						requestMessage.Headers.Authorization =
							new AuthenticationHeaderValue("Bearer", _accessToken);
						return Task.FromResult(0);
					}));

			var driveRequest = graphserviceClient
				.Drive
				.Request();
			var driveResponse = await driveRequest.GetAsync();

			var used = Math.Round(driveResponse.Quota.Used.Value / Math.Pow(2, 30), 0);
			Console.WriteLine($"Drive contains {used} GB of data");
		}
	}
}
