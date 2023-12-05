namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Xml.Linq;

	public class CatalogMetaData
	{
		public string Branch { get; set; } = "";

		public string CommitterMail { get; set; } = "";

		public string ContentType { get; set; } = "";

		public string Identifier { get; set; } = "";

		public bool IsPreRelease { get; set; }

		public string Name { get; set; } = "";

		public string ReleaseUri { get; set; } = "";

		public string Version { get; set; } = "";

		public static CatalogMetaData FromArtifact(string pathToArtifact)
		{
			CatalogMetaData meta;

			if (String.IsNullOrWhiteSpace(pathToArtifact))
			{
				throw new ArgumentNullException(nameof(pathToArtifact));
			}

			if (pathToArtifact.EndsWith(".dmapp", StringComparison.InvariantCultureIgnoreCase))
			{
				meta = CatalogMetaData.FromDmapp(pathToArtifact);
			}
			else if (pathToArtifact.EndsWith(".dmprotocol", StringComparison.InvariantCultureIgnoreCase))
			{
				meta = CatalogMetaData.FromDmprotocol(pathToArtifact);
			}
			else
			{
				throw new InvalidOperationException($"Invalid path to artifact. Expected a path that ends with .dmapp or .dmprotocol but received {pathToArtifact}");
			}

			return meta;
		}

		// Used during unit testing to assert data.
		public override bool Equals(object? obj)
		{
			return obj is CatalogMetaData data &&
				   Version == data.Version &&
				   Branch == data.Branch &&
				   IsPreRelease == data.IsPreRelease &&
				   Identifier == data.Identifier &&
				   Name == data.Name &&
				   CommitterMail == data.CommitterMail &&
				   ReleaseUri == data.ReleaseUri &&
				   ContentType == data.ContentType;
		}

		// Needed to match with Equals
		public override int GetHashCode()
		{
			return HashCode.Combine(Version, Branch, IsPreRelease, Identifier, Name, ContentType, CommitterMail, ReleaseUri);
		}

		private static CatalogMetaData FromDmapp(string pathToDmapp)
		{
			// Open as a ZIP file.
			/*AppInfo.xml
			 *
			 * <?xml version="1.0" encoding="utf-8"?>
<AppInfo>
  <DisplayName>COX Communications CISCO CBR-8 CCAP Platform Collector</DisplayName>
  <LastModifiedAt>2023-11-24T14:05:47</LastModifiedAt>
  <MinDmaVersion>10.0.9.0-9312</MinDmaVersion>
  <Name>COX Communications CISCO CBR-8 CCAP Platform Collector</Name>
  <AllowMultipleInstalledVersions>false</AllowMultipleInstalledVersions>
  <Version>0.0.0-CU1</Version>
</AppInfo>
			 *

			Description.txt

Bridge Technologies VB Probe Series package version: 0.0.0-CU2
---------------------------------
Package creation time: 2023-11-24 13:38:17
---------------------------------
File Versions:
Visio\skyline_Bridge Technologies VB Probe Series:0.0.0-CU2

			 */

			string appInfoRaw;

			using (var zipFile = ZipFile.OpenRead(pathToDmapp))
			{
				var foundFile = zipFile.Entries.FirstOrDefault(x => x.Name.Equals("AppInfo.xml", StringComparison.InvariantCulture));
				if (foundFile == null) throw new InvalidOperationException("Could not find AppInfo.xml in the .dmapp.");

				using (var stream = foundFile.Open())
				{
					using (var memoryStream = new StreamReader(stream))
					{
						appInfoRaw = memoryStream.ReadToEnd();
					}
				}
			}

			CatalogMetaData meta = new CatalogMetaData();
			var appInfo = XDocument.Parse(appInfoRaw);
			meta.Name = appInfo.Element("DisplayName")?.Value;
			meta.Version = appInfo.Element("Version")?.Value;
			return meta;
		}

		private static CatalogMetaData FromDmprotocol(string pathToDmprotocol)
		{
			// Description.txt
			/*
Protocol Name: Microsoft Platform
Protocol Version: 6.0.0.4_B2
			 * */

			string descriptionText;

			using (var zipFile = ZipFile.OpenRead(pathToDmprotocol))
			{
				var foundFile = zipFile.Entries.FirstOrDefault(x => x.Name.Equals("Description.txt", StringComparison.InvariantCulture));
				if (foundFile == null) throw new InvalidOperationException("Could not find Description.txt in the .dmapp.");

				using (var stream = foundFile.Open())
				{
					using (var memoryStream = new StreamReader(stream))
					{
						descriptionText = memoryStream.ReadToEnd();
					}
				}
			}

			CatalogMetaData meta = new CatalogMetaData();

			using(var reader = new StringReader(descriptionText))
			{
				var line = reader.ReadLine();
				var splitLine = line.Split(':');

				switch (splitLine[0])
				{
					case "Protocol Name":
						meta.Name = splitLine[1];
						break;
					case "Protocol Version":
						meta.Version = splitLine[1];
						break;
					default:
						break;
				}
			}

			return meta;
		}
	}
}