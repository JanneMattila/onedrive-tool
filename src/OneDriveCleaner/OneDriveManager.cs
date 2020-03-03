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
		private GraphServiceClient _graphserviceClient;
		private List<DriveItem> _items = new List<DriveItem>();

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
			_graphserviceClient = new GraphServiceClient(
				new DelegateAuthenticationProvider(
					(requestMessage) =>
					{
						requestMessage.Headers.Authorization =
							new AuthenticationHeaderValue("Bearer", _accessToken);
						return Task.FromResult(0);
					}));
		}

		public async Task ScanAsync()
		{
			var driveRequest = _graphserviceClient
				.Me
				.Drive
				.Request();
			var driveResponse = await driveRequest.GetAsync();

			var used = Math.Round(driveResponse.Quota.Used.Value / Math.Pow(2, 30), 0);
			Console.WriteLine($"Drive contains {used} GB of data");

			var rootRequest = _graphserviceClient
				.Me
				.Drive
				.Root
				.Children
				.Request();
			await ProcessFiles(rootRequest);
		}

		private async Task ProcessFiles(IDriveItemChildrenCollectionRequest childrenCollectionRequest)
		{
			var driveItems = await childrenCollectionRequest.GetAsync();
			var items = new List<DriveItem>(driveItems.CurrentPage);
			while(driveItems.NextPageRequest != null)
			{
				var request = driveItems
					.NextPageRequest;
				var response = await request.GetAsync();

				items.AddRange(response.CurrentPage);
			}

			foreach (var item in items)
			{
				if (item.Folder != null)
				{
					var request = _graphserviceClient
						.Me
						.Drive
						.Items[item.Id]
						.Children
						.Request();
					await ProcessFiles(request);
				}
			}
			_items.AddRange(items);
		}
	}
}
