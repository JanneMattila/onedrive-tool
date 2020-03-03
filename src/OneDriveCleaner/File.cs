using CsvHelper.Configuration.Attributes;

namespace OneDriveCleaner
{
	public class File
    {
		[Name("id")]
		public string Id { get; set; }

		[Name("name")]
		public string Name { get; set; }

		[Name("path")]
		public string Path { get; set; }

		[Name("uri")]
		public string Uri { get; set; }

		[Name("size")]
		public long Size { get; set; }

		[Name("mimetype")]
		public string MimeType { get; set; }

		[Name("sha1hash")]
		public string Sha1Hash { get; set; }
	}
}
