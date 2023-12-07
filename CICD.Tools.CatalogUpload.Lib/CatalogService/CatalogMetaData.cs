namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Xml.Linq;

	/// <summary>
	/// Represents all metadata for a package.
	/// </summary>
	public class CatalogMetaData
	{
		private bool artifactHadBuildNumber;

		/// <summary>
		/// The Branch/Range/Category this version belongs to. Defaults to "main"
		/// </summary>
		public string Branch { get; set; } = "main";

		/// <summary>
		/// The e-mail address of the Author, often the committer of a GIT Tag on the sourcecode making the package.
		/// </summary>
		public string CommitterMail { get; set; }

		/// <summary>
		/// The type of content, as understood by the ArtifactUpload.
		/// </summary>
		public string ContentType { get; set; }

		/// <summary>
		/// An global, readable, unique identifier for this package. This is often the URI to the sourcecode.
		/// </summary>
		public string Identifier { get; set; }

		/// <summary>
		/// The Name of the Package
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// A URI leading to the release notes of this package version.
		/// </summary>
		public string ReleaseUri { get; set; }

		/// <summary>
		///  The version of the package.
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// Creates a partial CataLogMetaData using any information it can from the artifact itself. Check the items for null and complete.
		/// </summary>
		/// <param name="pathToArtifact">Path to the artifact.</param>
		/// <returns>An instance of <see cref="CatalogMetaData"/>.></returns>
		/// <exception cref="ArgumentNullException">Provided path should not be null</exception>
		/// <exception cref="InvalidOperationException">Expected data was not present in the Artifact.</exception>
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

		/// <summary>
		/// Used during unit testing to assert data.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return obj is CatalogMetaData data &&
				   Version == data.Version &&
				   Branch == data.Branch &&
				   Identifier == data.Identifier &&
				   Name == data.Name &&
				   CommitterMail == data.CommitterMail &&
				   ReleaseUri == data.ReleaseUri &&
				   ContentType == data.ContentType;
		}

		/// <summary>
		/// Needed to match with Equals
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(Version, Branch, Identifier, Name, ContentType, CommitterMail, ReleaseUri);
		}

		/// <summary>
		/// Whether this is a pre-release or a full release. This is automatically decided from the Version and artifact content.
		/// </summary>
		public bool IsPreRelease()
		{
			// Might need to check for protocols how to handle this. "_BX" probably added to versions?
			// Should we allow a force set/override?

			if (!artifactHadBuildNumber)
			{
				// Not a version from .dmapp --> assume semantic versioning
				if (!Regex.IsMatch(Version, "^[0-9]+.[0-9]+.[0-9]+(-CU[0-9]+)?$"))
				{
					return Version.Contains('-');
				}
				else
				{
					// Versioning that dataminer uses with the -CU.
					return Version.StartsWith("0.0.0-");
				}
			}
			else
			{
				return true;
			}
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
			string contentType;

			using (var zipFile = ZipFile.OpenRead(pathToDmapp))
			{
				ZipArchiveEntry foundFile = zipFile.GetEntry("AppInfo.xml");
				if (foundFile == null) throw new InvalidOperationException("Could not find AppInfo.xml in the .dmapp.");

				using (var stream = foundFile.Open())
				{
					using (var memoryStream = new StreamReader(stream))
					{
						appInfoRaw = memoryStream.ReadToEnd();
					}
				}

				ContentType contentFromPackagContent = new ContentType(zipFile);
				contentType = contentFromPackagContent.Value;
			}

			CatalogMetaData meta = new CatalogMetaData();
			var appInfo = XDocument.Parse(appInfoRaw).Root;
			meta.Name = appInfo.Element("DisplayName")?.Value;

			var buildNumber = appInfo.Element("Build")?.Value;

			if (!String.IsNullOrWhiteSpace(buildNumber))
			{
				meta.artifactHadBuildNumber = true;
				// Throw away the CU version. If we have a build number it's a pre-release.
				string version = appInfo.Element("Version")?.Value;

				if (version != null)
				{
					if (version.Contains("-CU"))
					{
						// Throw away the CU version. If we have a build number it's a pre-release.
						version = version.Split('-')[0];
					}

					meta.Version = version + "-B" + buildNumber;
				}
			}
			else
			{
				meta.artifactHadBuildNumber = false;
				meta.Version = appInfo.Element("Version")?.Value;
			}

			meta.ContentType = contentType;
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

			using (var reader = new StringReader(descriptionText))
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

			meta.ContentType = "protocol";
			return meta;
		}
	}
}