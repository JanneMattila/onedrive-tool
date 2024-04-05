using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;

namespace OneDriveTool;

public class OneDriveManager
{
	private GraphServiceClient _graphServiceClient;
	private List<File> _items = new();
	private string _driveId;

	public OneDriveManager()
	{
	}

	public void AuthenticateUsingClient(string clientId)
	{
		var options = new InteractiveBrowserCredentialOptions
		{
			TenantId = "common",
			ClientId = clientId,
			RedirectUri = new Uri("http://localhost"),
		};

		var interactiveCredential = new InteractiveBrowserCredential(options);
		_graphServiceClient = new GraphServiceClient(interactiveCredential);
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
		var start = DateTime.Now;
		var drive = await _graphServiceClient.Me.Drive.GetAsync();
		_driveId = drive.Id;
		var root = await _graphServiceClient
			.Drives[_driveId]
			.Items["root"]
			.Children
			.GetAsync();

		var used = Math.Round(drive.Quota.Used.Value / Math.Pow(2, 30), 0);
		Console.WriteLine($"Drive contains {used} GB of data");

		await ProcessFiles(root.Value, string.Empty, 1);

		using var stream = new FileStream("files.csv", FileMode.Create);
		using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			Encoding = Encoding.UTF8
		});
		csv.WriteRecords(_items);
		Console.WriteLine($"Scanning took {Math.Ceiling((DateTime.Now - start).TotalSeconds/60)} minutes.");
	}

	private async Task ProcessFiles(List<DriveItem> items, string path, int level)
	{
		Console.WriteLine(path);

		var index = 0;
		foreach (var fileItem in items.Where(o => o.Folder == null))
		{
			var file = new File
			{
				Id = fileItem.Id,
				Name = fileItem.Name,
				Uri = fileItem.WebUrl,
				Path = path,
				Size = fileItem.Size.GetValueOrDefault(),
				MimeType = fileItem.File?.MimeType,
				Sha1Hash = fileItem.File?.Hashes?.Sha1Hash,
				Sha256Hash = fileItem.File?.Hashes?.Sha256Hash
			};
			_items.Add(file);

			index++;
			if (index % 1000 == 0)
			{
				Console.WriteLine($"{path}/{fileItem.Name}");
			}
		}

		foreach (var folderItem in items.Where(o => o.Folder != null))
		{
			var folder = await _graphServiceClient
				.Drives[_driveId]
				.Items[folderItem.Id]
				.Children
				.GetAsync();
			await ProcessFiles(folder.Value, $"{path}/{folderItem.Name}", level + 1);
		}
	}
}
