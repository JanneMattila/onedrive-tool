using CsvHelper.Configuration.Attributes;

namespace OneDriveTool;

public class File
{
	[Name("ID")]
	public string Id { get; set; }

	[Name("Name")]
	public string Name { get; set; }

	[Name("Path")]
	public string Path { get; set; }

	[Name("URI")]
	public string Uri { get; set; }

	[Name("Size")]
	public long Size { get; set; }

	[Name("MimeType")]
	public string MimeType { get; set; }

	[Name("SHA1Hash")]
	public string Sha1Hash { get; set; }

	[Name("SHA256Hash")]
	public string Sha256Hash { get; set; }
}
