using System.Globalization;
using System.IO;
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
		List<OneDriveFile> items = ReadOneDriveExportFile(file);

		_logger.LogInformation("{Count} total files to analyze.", items.Count);

		var hashes = new Dictionary<string, List<OneDriveFile>>();
		foreach (var item in items)
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
		_logger.LogInformation("{Duplicates} duplicate files to analyze.", duplicates);

		const int top = 25;
		var duplicateList = hashes
			.OrderByDescending(h => h.Value.Count)
			.Take(top);
		var topDuplicates = string.Join(", ", duplicateList.Select(h => h.Value.Count));
		_logger.LogInformation("Top {Top} duplicate counts: {TopDuplicates}", top, topDuplicates);

		foreach (var item in duplicateList)
		{
			_logger.LogInformation("{Count} copies:", item.Value.Count);

			var duplicateFiles = items.Where(o => o.Sha1Hash == item.Key);
			foreach(var duplicateFile in  duplicateFiles)
			{
				_logger.LogInformation("{Path}/{Name}",
					duplicateFile.Path, duplicateFile.Name);
			}
		}
	}

	public async Task ExportAsync(string file)
	{
		var start = DateTime.Now;
		var drive = await _graphServiceClient.Me.Drive.GetAsync();
		_driveId = drive.Id;

		var childItems = await FetchChildItems("root");

		var used = Math.Round(drive.Quota.Used.Value / Math.Pow(2, 30), 0);
		_logger.LogInformation("Drive contains {Used} GB of data", used);

		List<OneDriveFile> items = [];
		await ProcessFiles(items, childItems, string.Empty, 1);

		WriteOneDriveExportFile(file, items);
		_logger.LogInformation("Scanning took {Time} minutes.", Math.Ceiling((DateTime.Now - start).TotalSeconds / 60));
	}

	public void Scan(string inputFile, string folder, string outputFile)
	{
		List<OneDriveFile> items = ReadOneDriveExportFile(inputFile);
		_logger.LogInformation("{Count} files in OneDrive.", items.Count);

		var hashes = items.Select(o => o.Sha1Hash).ToHashSet();

		List<LocalFile> localFiles = ReadLocalFile(outputFile);

		_logger.LogInformation("Enumerating files from folder '{Folder}' and all the sub-folders. This might take a while.", folder);

		var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories).ToList();

		var filesToProcess = files.Count;
		_logger.LogInformation("{Count} local files to analyze.", files.Count);

		var alreadyProcessed = 0;
		foreach (var alreadyProcessedFile in localFiles)
		{
			if (files.Contains(Path.Combine(alreadyProcessedFile.Path, alreadyProcessedFile.Name)))
			{
				_logger.LogInformation("File is already processed: {File}", alreadyProcessedFile.Name);
				files.Remove(Path.Combine(alreadyProcessedFile.Path, alreadyProcessedFile.Name));
				alreadyProcessed++;
			}
		}

		if (alreadyProcessed > 0)
		{
			_logger.LogInformation("{AlreadyProcessed} out of {TotalFiles} files processed which is {Percent} %. Still {ToProcess} files to process.",
				alreadyProcessed, filesToProcess, Math.Round((float)alreadyProcessed / filesToProcess * 100, 0), files.Count);
		}

		using var sha1 = SHA1.Create();
		foreach (var item in files)
		{
			try
			{
				using var fileStream = File.Open(item, FileMode.Open, FileAccess.Read);
				byte[] hashValue = sha1.ComputeHash(fileStream);
				var hash = BitConverter.ToString(hashValue).Replace("-", string.Empty);
				var inOneDrive = hashes.Contains(hash);
				if (inOneDrive)
				{
					_logger.LogInformation("{AlreadyProcessed} / {TotalFiles} - {Percent} %: File is already in OneDrive: {File}",
						alreadyProcessed, filesToProcess, Math.Round((float)alreadyProcessed / filesToProcess * 100, 0), item);
				}
				else
				{
					_logger.LogInformation("{AlreadyProcessed} / {TotalFiles} - {Percent} %: New file: {File}",
						alreadyProcessed, filesToProcess, Math.Round((float)alreadyProcessed / filesToProcess * 100, 0), item);
				}

				WriteLocalFile(outputFile, localFiles, item, hash, fileStream.Length, inOneDrive);
				alreadyProcessed++;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Could not process {File}", item);
			}
		}
	}

	private List<OneDriveFile> ReadOneDriveExportFile(string inputFile)
	{
		using StreamReader reader = new StreamReader(inputFile);
		using CsvReader csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			Encoding = Encoding.UTF8
		});
		return csv.GetRecords<OneDriveFile>().ToList();
	}

	private void WriteOneDriveExportFile(string file, List<OneDriveFile> items)
	{
		using FileStream stream = new FileStream(file, FileMode.Create);
		using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		using CsvWriter csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			Encoding = Encoding.UTF8
		});
		csv.WriteRecords(items);
	}

	private List<LocalFile> ReadLocalFile(string file)
	{
		List<LocalFile> localFiles = [];
		if (File.Exists(file))
		{
			using var outputFileReader = new StreamReader(file);
			using var outputFileCSV = new CsvReader(outputFileReader, new CsvConfiguration(CultureInfo.CurrentCulture)
			{
				Delimiter = ";",
				HasHeaderRecord = true,
				Encoding = Encoding.UTF8
			});
			localFiles = outputFileCSV.GetRecords<LocalFile>().ToList();
		}
		return localFiles;
	}

	private void WriteLocalFile(string outputFile, List<LocalFile> localFiles, string item, string hash, long length, bool inOneDrive)
	{
		var localFile = new LocalFile
		{
			Name = Path.GetFileName(item),
			Path = Path.GetDirectoryName(item),
			Size = length,
			Sha1Hash = hash,
			InOneDrive = inOneDrive
		};
		localFiles.Add(localFile);

		using var stream = new FileStream(outputFile, FileMode.Create);
		using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true,
			Encoding = Encoding.UTF8
		});
		csv.WriteRecords(localFiles);
	}

	private async Task ProcessFiles(List<OneDriveFile> output, List<DriveItem> items, string path, int level)
	{
		_logger.LogInformation("{Path} - {Count} items", path, items.Count);

		var index = 0;
		var files = items.Where(o => o.Folder == null).ToList();
		foreach (var fileItem in files)
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
			output.Add(file);

			index++;
			if (index % 100 == 0)
			{
				_logger.LogInformation("{Index} / {Total}: {File}", index, files.Count, $"{path}/{fileItem.Name}");
			}
		}

		foreach (var folderItem in items.Where(o => o.Folder != null))
		{
			var childItems = await FetchChildItems(folderItem.Id);

			await ProcessFiles(output, childItems, $"{path}/{folderItem.Name}", level + 1);
		}
	}

	private async Task<List<DriveItem>> FetchChildItems(string id)
	{
		var folderResponse = await _graphServiceClient
			.Drives[_driveId]
			.Items[id]
			.Children
			.GetAsync();

		var added = 0;
		var items = new List<DriveItem>();
		var pageIterator = PageIterator<DriveItem, DriveItemCollectionResponse>.CreatePageIterator(_graphServiceClient, folderResponse,
			(driveItem) =>
			{
				items.Add(driveItem);

				added++;

				if (added % 100 == 0)
				{
					_logger.LogInformation("Enumerating {Path} - {Count} items found", driveItem.Name, added);
				}
				return true;
			});

		await pageIterator.IterateAsync();
		return items;
	}
}
