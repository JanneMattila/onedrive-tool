using CsvHelper.Configuration.Attributes;

namespace OneDriveTool;

public class LocalFile
{
	[Name("Name")]
	public string Name { get; set; }

	[Name("Path")]
	public string Path { get; set; }

	[Name("Size")]
	public long Size { get; set; }

	[Name("SHA1Hash")]
	public string Sha1Hash { get; set; }

	[Name("InOneDrive")]
	public bool InOneDrive { get; set; }
}
