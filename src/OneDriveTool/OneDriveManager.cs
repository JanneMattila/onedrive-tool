using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace OneDriveTool;

public class OneDriveManager
{
	private readonly ILogger<OneDriveManager> _logger;
	private GraphServiceClient _graphServiceClient;
	private List<OneDriveFile> _items = [];
	private string _driveId;

	public OneDriveManager(ILogger<OneDriveManager> logger)
	{
		_logger = logger;
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

	public void Analyze(string file)
	{
		using var reader = new StreamReader(file);
		using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			Encoding = Encoding.UTF8
		});
		_items = csv.GetRecords<OneDriveFile>().ToList();

		_logger.LogInformation("{Count} files to analyze.", _items.Count);

		var hashes = new Dictionary<string, List<OneDriveFile>>();
		foreach (var item in _items)
		{
			if (!hashes.TryGetValue(item.Sha1Hash, out var value))
			{
				value = ([]);
				hashes.Add(item.Sha1Hash, value);
			}

			value.Add(item);
		}

		var duplicates = hashes
			.Where(h => h.Value.Count > 1)
			.Sum(h => h.Value.Count - 1);
		_logger.LogInformation("{Duplicates} files to analyze.", duplicates);

		const int top = 25;
		var topDuplicates = string.Join(", ", hashes
			.OrderByDescending(h => h.Value.Count)
			.Select(h => h.Value.Count)
			.Take(top));
		_logger.LogInformation("Top {Top}  duplicate counts: {TopDuplicates}", top, topDuplicates);
	}

	public async Task ExportAsync(string file)
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
		_logger.LogInformation("Drive contains {Used} GB of data", used);

		await ProcessFiles(root.Value, string.Empty, 1);

		using var stream = new FileStream(file, FileMode.Create);
		using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			Encoding = Encoding.UTF8
		});
		csv.WriteRecords(_items);
		_logger.LogInformation("Scanning took {Time} minutes.", Math.Ceiling((DateTime.Now - start).TotalSeconds / 60));
	}

	public void Scan(string file, string folder)
	{
		using var reader = new StreamReader(file);
		using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			Encoding = Encoding.UTF8
		});
		_items = csv.GetRecords<OneDriveFile>().ToList();

		var hashes = _items.Select(o => o.Sha1Hash).ToHashSet();
		var localFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

		_logger.LogInformation("{Count} local files to analyze.", localFiles.Length);

		using var sha1 = SHA1.Create();
		foreach (var item in localFiles)
		{
			try
			{
				using var fileStream = File.Open(item, FileMode.Open);
				byte[] hashValue = sha1.ComputeHash(fileStream);
				var hash = BitConverter.ToString(hashValue).Replace("-", string.Empty);
				if (hashes.Contains(hash))
				{
					_logger.LogInformation("File is already in OneDrive: {File}", item);
				}
				else
				{
					_logger.LogInformation("New file: {File}", item);
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Could not process {File}", item);
			}
		}
	}

	private async Task ProcessFiles(List<DriveItem> items, string path, int level)
	{
		Console.WriteLine(path);

		var index = 0;
		foreach (var fileItem in items.Where(o => o.Folder == null))
		{
			var file = new OneDriveFile
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
				_logger.LogInformation("{File}", $"{path}/{fileItem.Name}");
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
