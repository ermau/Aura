using System;

namespace Aura.Data
{
	public record ContentLicense
	{
		public string Name
		{
			get;
			init;
		}

		public string Url
		{
			get;
			init;
		}

		public override string ToString ()
		{
			return Name ?? Url;
		}

		public static ContentLicense GetLicense (string content)
		{
			if (String.IsNullOrWhiteSpace (content))
				return new ContentLicense ();

			if (Uri.TryCreate (content, UriKind.Absolute, out Uri uri)) {
				string name = null;
				if (uri.Host == "creativecommons.org") {
					name = GetCreativeCommonsName (uri);
				}

				return new ContentLicense {
					Url = content,
					Name = name
				};
			} else {
				return new ContentLicense { Name = content };
			}
		}

		private static string GetCreativeCommonsName (Uri uri)
		{
			string name = null;
			string[] parts = uri.AbsolutePath.Split ('/');
			if (parts.Length >= 4) {
				string root = parts[1].ToLower ();
				if (root == "licenses")
					name = $"CC {parts[2].ToUpper ()} {parts[3]}";
				else if (root == "publicdomain" && parts[2].ToLower () == "zero")
					name = $"CC0 {parts[3]}";
			}

			return name;
		}
	}
}
