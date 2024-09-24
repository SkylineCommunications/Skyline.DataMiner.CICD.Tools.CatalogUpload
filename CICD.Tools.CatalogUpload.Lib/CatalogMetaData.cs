namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Xml.Linq;
	using YamlDotNet.Serialization;
	using YamlDotNet.Serialization.NamingConventions;

	using Skyline.DataMiner.CICD.FileSystem;
	using YamlDotNet.Core;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.CatalogService;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using static System.Net.Mime.MediaTypeNames;
	using System.Threading.Tasks;

	/// <summary>
	/// Represents all metadata for a package.
	/// </summary>
	public class CatalogMetaData
	{
		public CatalogMetaData()
		{
			Owners = new List<CatalogOwner>();
			Version = new CatalogVersionMetaData();
			Tags = new List<string>();
		}

		private bool artifactHadBuildNumber;

		/// <summary>
		/// An global, readable, unique identifier for this package. This is the GUID defined in the Catalog.
		/// </summary>
		public string CatalogIdentifier { get; set; }

		/// <summary>
		/// The type of content, as understood by the ArtifactUpload.
		/// </summary>
		public string ContentType { get; set; }

		/// <summary>
		///  The URI to the sourcecode.
		/// </summary>
		public string SourceCodeUri { get; set; }

		/// <summary>
		/// The Name of the Package
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the short description for the catalog entry.
		/// </summary>
		/// <value>
		/// A brief description of the catalog entry.
		/// </value>
		public string ShortDescription { get; set; }

		/// <summary>
		/// Gets or sets the URL to the documentation related to the catalog entry.
		/// </summary>
		/// <value>
		/// A string representing the documentation URL.
		/// </value>
		public string DocumentationUrl { get; set; }

		/// <summary>
		/// Gets or sets the owner information for the catalog entry.
		/// </summary>
		/// <value>
		/// The <see cref="CatalogOwner"/> that represents the owner of the catalog entry.
		/// </value>
		public List<CatalogOwner> Owners { get; set; }

		/// <summary>
		/// Gets or sets the version metadata for the catalog entry.
		/// </summary>
		/// <remarks>
		/// This can be left empty if the version is not being updated.
		/// </remarks>
		/// <value>
		/// The <see cref="CatalogVersionMetaData"/> that contains the version details of the catalog entry.
		/// </value>
		public CatalogVersionMetaData Version { get; set; }

		public List<string> Tags { get; set; }

		public string PathToReadme { get; set; }

		public string PathToImages { get; set; }

		/// <summary>
		/// Creates a partial CataLogMetaData using any information it can from the artifact itself. Check the items for null and complete.
		/// </summary>
		/// <param name="pathToArtifact">Path to the artifact.</param>
		/// <param name="pathToReadme"></param>
		/// <param name="pathToImages"></param>
		/// <returns>An instance of <see cref="CatalogMetaData"/>.></returns>
		/// <exception cref="ArgumentNullException">Provided path should not be null</exception>
		/// <exception cref="InvalidOperationException">Expected data was not present in the Artifact.</exception>
		public static CatalogMetaData FromArtifact(string pathToArtifact, string pathToReadme = null, string pathToImages = null)
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
		/// 
		/// </summary>
		/// <param name="startPath"></param>
		/// <param name="pathToReadme"></param>
		/// <param name="pathToImages"></param>
		/// <returns></returns>
		public static CatalogMetaData FromCatalogYaml(string startPath, string pathToReadme = null, string pathToImages = null)
		{
			var meta = new CatalogMetaData();
			meta.SearchAndApplyCatalogYaml(FileSystem.Instance, startPath);

			if (pathToReadme == null)
			{
				meta.SearchAndApplyReadMe(FileSystem.Instance, startPath);
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
		/// Used during unit testing to assert data.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return obj is CatalogMetaData data &&
				   Version == data.Version &&
				   SourceCodeUri == data.SourceCodeUri &&
				   Name == data.Name &&
				   ContentType == data.ContentType;
		}

		/// <summary>
		/// Needed to match with Equals
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(SourceCodeUri, Name, ContentType, CatalogIdentifier, ShortDescription);
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
				if (!Regex.IsMatch(Version.Value, "^[0-9]+.[0-9]+.[0-9]+(-CU[0-9]+)?$"))
				{
					return Version.Value.Contains('-');
				}
				else
				{
					// Versioning that dataminer uses with the -CU.
					return Version.Value.StartsWith("0.0.0-");
				}
			}
			else
			{
				return true;
			}
		}

		public async Task<byte[]> ToCatalogZipAsync()
		{
			var serializer = new SerializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.Build();

			CatalogYaml catalogYaml = new CatalogYaml()
			{
				Id = CatalogIdentifier,
				Documentation_url = DocumentationUrl,
				Short_description = ShortDescription,
				Title = Name,
				Type = ContentType,
				Source_code_url = SourceCodeUri
			};

			if (Owners != null)
			{
				Owners.ForEach(o => catalogYaml.Owners.Add(new CatalogYamlOwner() { Name = o.Name, Email = o.Email, Url = o.Url }));
			}

			if (Tags != null)
			{
				Tags.ForEach(catalogYaml.Tags.Add);
			}

			var yaml = serializer.Serialize(catalogYaml);

			// Create a zip file in memory that contains manifest.yml, README.md, and an Images folder as expected by the API
			using (var memoryStream = new MemoryStream())
			{
				using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
				{
					// Add manifest.yml
					var manifestEntry = archive.CreateEntry("manifest.yml");
					using (var entryStream = manifestEntry.Open())

					using (var streamWriter = new StreamWriter(entryStream))
					{
						await streamWriter.WriteAsync(yaml);
						await streamWriter.FlushAsync(); // Ensure everything is written
					}

					// Add README.md
					if (PathToReadme != null)
					{
						var readmeEntry = archive.CreateEntry("README.md");
						using (var entryStream = readmeEntry.Open())
						using (var fileStream = File.OpenRead(PathToReadme))
						{
							await fileStream.CopyToAsync(entryStream);
						}
					}

					// Add Images folder
					if (PathToImages != null)
					{
						if (Directory.Exists(PathToImages))
						{
							var imageFiles = Directory.GetFiles(PathToImages);
							foreach (var imageFile in imageFiles)
							{
								var imageEntry = archive.CreateEntry($"Images/{Path.GetFileName(imageFile)}");
								using (var entryStream = imageEntry.Open())
								using (var fileStream = File.OpenRead(imageFile))
								{
									await fileStream.CopyToAsync(entryStream);
								}
							}
						}
					}
				}

				// Convert zip file to byte array
				return memoryStream.ToArray();
			}
		}

		public bool SearchAndApplyReadMe(IFileSystem fs, string startPath)
		{
			string foundReadme;
			if (fs.Directory.IsDirectory(startPath))
			{
				foundReadme = RecursiveFindClosestReadmeMd(fs, startPath, 5);
			}
			else if (startPath.EndsWith(".md"))
			{
				foundReadme = startPath;
			}
			else
			{
				var directoryForReadme = fs.File.GetParentDirectory(startPath);
				foundReadme = RecursiveFindClosestReadmeMd(fs, directoryForReadme, 5);
			}

			PathToReadme = foundReadme;
			if (foundReadme == null) return false;

			string foundImages;
			var directoryForImages = fs.File.GetParentDirectory(PathToReadme);
			foundImages = RecursiveFindClosestImages(fs, directoryForImages, 5);

			PathToImages = foundImages;
			return true;
		}

		public bool SearchAndApplyCatalogYaml(IFileSystem fs, string startPath)
		{
			string foundYaml;
			if (fs.Directory.IsDirectory(startPath))
			{
				foundYaml = RecursiveFindClosestReadmeMd(fs, startPath, 5);
			}
			else if (startPath.EndsWith(".yml"))
			{
				foundYaml = startPath;
			}
			else
			{
				var directory = fs.File.GetParentDirectory(startPath);
				foundYaml = RecursiveFindClosestReadmeMd(fs, directory, 5);
			}

			if (foundYaml == null) return false;

			// use a yaml parser to grab the yaml file.
			var deserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml 
			.Build();

			var yml = fs.File.ReadAllText(foundYaml);
			var p = deserializer.Deserialize<CatalogYaml>(yml);

			// Apply all the settings provided in the yaml file.
			// Overwrite already written arguments.
			if (!String.IsNullOrWhiteSpace(p.Id)) CatalogIdentifier = p.Id;
			if (!String.IsNullOrWhiteSpace(p.Type)) ContentType = p.Type;
			if (!String.IsNullOrWhiteSpace(p.Title)) Name = p.Title;

			if (!String.IsNullOrWhiteSpace(p.Short_description)) ShortDescription = p.Short_description;
			if (!String.IsNullOrWhiteSpace(p.Source_code_url)) SourceCodeUri = p.Source_code_url;
			if (p.Owners != null)
			{
				p.Owners.ForEach(o => { Owners.Add(new CatalogOwner() { Name = o.Name, Email = o.Email, Url = o.Url }); });
			}

			if (p.Tags != null)
			{
				p.Tags.ForEach(Tags.Add);
			}

			return true;
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

					meta.Version.Value = version + "-B" + buildNumber;
				}
			}
			else
			{
				meta.artifactHadBuildNumber = false;
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

			meta.ContentType = "protocol";
			return meta;
		}

		private string RecursiveFindClosestCatalogYaml(IFileSystem fs, string directory, int maxRecurse)
		{
			if (maxRecurse-- <= 0)
			{
				return null;
			}

			foreach (var file in fs.Directory.EnumerateFiles(directory))
			{
				string fileName = fs.Path.GetFileName(file);
				if (fileName.Equals("manifest.yml", StringComparison.InvariantCultureIgnoreCase) || fileName.Equals("catalog.yml"))
				{
					return file;
				}
			}

			var parent = fs.Directory.GetParentDirectory(directory);
			return RecursiveFindClosestCatalogYaml(fs, parent, maxRecurse);
		}

		private string RecursiveFindClosestReadmeMd(IFileSystem fs, string directory, int maxRecurse)
		{
			if (maxRecurse-- <= 0)
			{
				return null;
			}

			foreach (var file in fs.Directory.EnumerateFiles(directory))
			{
				string fileName = fs.Path.GetFileName(file);
				if (fileName.Equals("readme.md", StringComparison.InvariantCultureIgnoreCase))
				{
					return file;
				}
			}

			var parent = fs.Directory.GetParentDirectory(directory);
			return RecursiveFindClosestReadmeMd(fs, parent, maxRecurse);
		}

		private string RecursiveFindClosestImages(IFileSystem fs, string directory, int maxRecurse)
		{
			if (maxRecurse-- <= 0)
			{
				return null;
			}

			foreach (var subdir in fs.Directory.EnumerateDirectories(directory))
			{
				string fileName = fs.Path.GetDirectoryName(subdir);
				if (fileName.Equals("images", StringComparison.InvariantCultureIgnoreCase))
				{
					return subdir;
				}
			}

			var parent = fs.Directory.GetParentDirectory(directory);
			return RecursiveFindClosestImages(fs, parent, maxRecurse);
		}
	}

	/// <summary>
	/// Represents the owner of the catalog.
	/// </summary>
	public class CatalogOwner
	{
		/// <summary>
		/// Gets or sets the name of the catalog owner.
		/// </summary>
		/// <value>
		/// A string representing the owner's name.
		/// </value>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the email of the catalog owner.
		/// </summary>
		/// <value>
		/// A string representing the owner's email address.
		/// </value>
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the URL of the catalog owner.
		/// </summary>
		/// <value>
		/// A string representing the owner's website or URL.
		/// </value>
		public string Url { get; set; }

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj is CatalogOwner other)
			{
				return Name == other.Name && Email == other.Email && Url == other.Url;
			}
			return false;
		}

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Email, Url);
		}
	}

	/// <summary>
	/// Contains metadata related to the version of the package.
	/// </summary>
	public class CatalogVersionMetaData
	{
		/// <summary>
		/// Gets or sets the URI leading to the release notes of this package version.
		/// </summary>
		/// <value>
		/// A string representing the URI to the release notes.
		/// </value>
		public string ReleaseUri { get; set; }

		/// <summary>
		/// Gets or sets the version of the package.
		/// </summary>
		/// <value>
		/// A string representing the version number of the package.
		/// </value>
		public string Value { get; set; }

		/// <summary>
		/// Gets or sets the description of the version.
		/// </summary>
		/// <value>
		/// A string representing the description of the version.
		/// </value>
		public string VersionDescription { get; set; }

		/// <summary>
		/// Gets or sets the email address of the committer.
		/// </summary>
		/// <value>
		/// A string representing the committer's email address.
		/// </value>
		public string CommitterMail { get; set; }

		/// <summary>
		/// The Branch/Range/Category this version belongs to. Defaults to "main"
		/// </summary>
		public string Branch { get; set; } = "main";

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj is CatalogVersionMetaData other)
			{
				return ReleaseUri == other.ReleaseUri &&
					   Value == other.Value &&
					   VersionDescription == other.VersionDescription &&
					   Branch == other.Branch &&
					   CommitterMail == other.CommitterMail;
			}
			return false;
		}

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(ReleaseUri, Value, VersionDescription, Branch, CommitterMail);
		}
	}
}