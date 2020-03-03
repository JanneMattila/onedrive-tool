using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace OneDriveCleaner
{
	public class OneDriveManager
    {
		private string _accessToken;
		private GraphServiceClient _graphserviceClient;
		private List<File> _items = new List<File>();

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

		public void Analyze()
		{
			using var reader = new StreamReader("files.csv");
			using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
			_items = csv.GetRecords<File>().ToList();

			Console.WriteLine($"{_items.Count} files to analyze.");
			var hashes = new Dictionary<string, List<File>>();
			foreach (var item in _items)
			{
				if (!hashes.ContainsKey(item.Sha1Hash))
				{
					hashes.Add(item.Sha1Hash, new List<File>());
				}
				hashes[item.Sha1Hash].Add(item);
			}

			var duplicates = hashes
				.Where(h => h.Value.Count > 1)
				.Sum(h => h.Value.Count - 1);
			Console.WriteLine($"{duplicates} total duplicate files.");

			const int top = 25;
			var topDuplicates = string.Join(", ", hashes
				.OrderByDescending(h => h.Value.Count)
				.Select(h => h.Value.Count)
				.Take(top));
			Console.WriteLine($"Top {top} duplicate counts: {topDuplicates}.");
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
			await ProcessFiles(rootRequest, string.Empty);

			using var writer = new StreamWriter("files.csv");
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			csv.WriteRecords(_items);
		}

		private async Task ProcessFiles(IDriveItemChildrenCollectionRequest childrenCollectionRequest, string path)
		{
			var driveItems = await childrenCollectionRequest.GetAsync();
			var items = new List<DriveItem>(driveItems.CurrentPage);
			while(driveItems.NextPageRequest != null)
			{
				var request = driveItems
					.NextPageRequest;
				var response = await request.GetAsync();

				items.AddRange(response.CurrentPage);
				driveItems = response;
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
					Console.Write("O");
					await ProcessFiles(request, $"{path}/{item.Name}");
				}
				else
				{
					var file = new File
					{
						Id = item.Id,
						Name = item.Name,
						Uri = item.WebUrl,
						Path = path,
						Size = item.Size.GetValueOrDefault(),
						MimeType = item.File?.MimeType,
						Sha1Hash = item.File?.Hashes?.Sha1Hash
					};
					_items.Add(file);
					Console.Write("o");
				}
			}
		}
	}
}
