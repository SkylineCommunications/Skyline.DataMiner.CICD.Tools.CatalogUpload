namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.Collections.Generic;
	using System.IO.Compression;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	using Skyline.DataMiner.CICD.FileSystem;

	/// <summary>
	/// A factory class responsible for creating instances of <see cref="CatalogMetaData"/>.
	/// </summary>
	public class CatalogMetaDataFactory : ICatalogMetaDataFactory
	{
		/// <summary>
		/// Creates a partial CataLogMetaData using any information it can from the artifact itself. Check the items for null and complete.
		/// </summary>
		/// <param name="pathToArtifact">Path to the artifact.</param>
		/// <param name="pathToReadme"></param>
		/// <param name="pathToImages"></param>
		/// <returns>An instance of <see cref="CatalogMetaData"/>.></returns>
		/// <exception cref="ArgumentNullException">Provided path should not be null</exception>
		/// <exception cref="InvalidOperationException">Expected data was not present in the Artifact.</exception>
		public CatalogMetaData FromArtifact(string pathToArtifact, string pathToReadme = null, string pathToImages = null)
		{
			CatalogMetaData meta;

			if (String.IsNullOrWhiteSpace(pathToArtifact))
			{
				throw new ArgumentNullException(nameof(pathToArtifact));
			}

			if (pathToArtifact.EndsWith(".dmapp", StringComparison.InvariantCultureIgnoreCase))
			{
				meta = CatalogMetaDataFactory.FromDmapp(pathToArtifact);
			}
			else if (pathToArtifact.EndsWith(".dmprotocol", StringComparison.InvariantCultureIgnoreCase))
			{
				meta = CatalogMetaDataFactory.FromDmprotocol(pathToArtifact);
			}
			else
			{
				throw new InvalidOperationException($"Invalid path to artifact. Expected a path that ends with .dmapp or .dmprotocol but received {pathToArtifact}");
			}

			if (pathToReadme == null)
			{
				meta.SearchAndApplyReadMe(FileSystem.Instance, pathToArtifact);
			}
			else
			{
				meta.PathToReadme = pathToReadme;
			}

			if (pathToImages != null)
			{
				meta.PathToImages = pathToImages;
			}

			return meta;
		}

		/// <summary>
		/// Creates a <see cref="CatalogMetaData"/> instance using information from a catalog YAML file.
		/// </summary>
		/// <param name="fs">The file system interface used for accessing files and directories.</param>
		/// <param name="startPath">The starting directory or file path for the catalog YAML file.</param>
		/// <param name="pathToReadme">Optional. The path to the README file. If null, the method will attempt to locate the README.</param>
		/// <param name="pathToImages">Optional. The path to the images directory. If null, no images path will be set.</param>
		/// <returns>A <see cref="CatalogMetaData"/> instance populated with the catalog YAML data.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a catalog.yml or manifest.yml file cannot be found within the provided directory, file, or parent directories.
		/// </exception>
		public CatalogMetaData FromCatalogYaml(IFileSystem fs, string startPath, string pathToReadme = null, string pathToImages = null)
		{
			var meta = new CatalogMetaData();
			if (!meta.SearchAndApplyCatalogYaml(fs, startPath))
				throw new InvalidOperationException("Unable to locate a catalog.yml or manifest.yml file within the provided directory/file or up to 5 parent directories.");

			if (pathToReadme == null)
			{
				meta.SearchAndApplyReadMe(fs, startPath);
			}
			else
			{
				meta.PathToReadme = pathToReadme;
			}

			if (pathToImages != null)
			{
				meta.PathToImages = pathToImages;
			}
			return meta;
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
				meta.ArtifactHadBuildNumber = true;
				// Throw away the CU version. If we have a build number it's a pre-release.
				string version = appInfo.Element("Version")?.Value;

				if (version != null)
				{
					if (version.Contains("-CU"))
					{
						// Throw away the CU version. If we have a build number it's a pre-release.
						version = version.Split('-')[0];
					}

					meta.Version.Value = version + "-B" + buildNumber;
				}
			}
			else
			{
				meta.ArtifactHadBuildNumber = false;
				meta.Version.Value = appInfo.Element("Version")?.Value;
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
						meta.Version.Value = splitLine[1];
						break;

					default:
						break;
				}
			}

			meta.ContentType = "connector";
			return meta;
		}
	}
}
