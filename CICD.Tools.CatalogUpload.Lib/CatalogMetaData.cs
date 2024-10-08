namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;

	using Skyline.DataMiner.CICD.FileSystem;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.CatalogService;

	using YamlDotNet.Serialization;
	using YamlDotNet.Serialization.NamingConventions;

	/// <summary>
	/// Represents all metadata for a package.
	/// </summary>
	public class CatalogMetaData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CatalogMetaData"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor initializes default values for the owners, version metadata, and tags of the catalog entry.
		/// </remarks>
		public CatalogMetaData()
		{
			Owners = new List<CatalogOwner>();
			Version = new CatalogVersionMetaData();
			Tags = new List<string>();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the artifact had a build number.
		/// </summary>
		/// <value>
		/// True if the artifact had a build number; otherwise, false.
		/// </value>
		public bool ArtifactHadBuildNumber { get; set; }

		/// <summary>
		/// A global, readable, unique identifier for this package. This is the GUID defined in the Catalog.
		/// </summary>
		public string CatalogIdentifier { get; set; }

		/// <summary>
		/// The type of content, as understood by the ArtifactUpload.
		/// </summary>
		public string ContentType { get; set; }

		/// <summary>
		/// Gets or sets the URL to the documentation related to the catalog entry.
		/// </summary>
		/// <value>
		/// A string representing the documentation URL.
		/// </value>
		public string DocumentationUrl { get; set; }

		/// <summary>
		/// The Name of the Package
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the owner information for the catalog entry.
		/// </summary>
		/// <value>
		/// The <see cref="CatalogOwner"/> that represents the owner of the catalog entry.
		/// </value>
		public List<CatalogOwner> Owners { get; set; }

		/// <summary>
		/// Gets or sets the path to the images related to the catalog entry.
		/// </summary>
		/// <value>
		/// A string representing the path to the images.
		/// </value>
		public string PathToImages { get; set; }

		/// <summary>
		/// Gets or sets the path to the readme file related to the catalog entry.
		/// </summary>
		/// <value>
		/// A string representing the path to the readme file.
		/// </value>
		public string PathToReadme { get; set; }

		/// <summary>
		/// Gets or sets the short description for the catalog entry.
		/// </summary>
		/// <value>
		/// A brief description of the catalog entry.
		/// </value>
		public string ShortDescription { get; set; }

		/// <summary>
		///  The URI to the sourcecode.
		/// </summary>
		public string SourceCodeUri { get; set; }

		/// <summary>
		/// Gets or sets the tags associated with the catalog entry.
		/// </summary>
		/// <value>
		/// A list of strings representing the tags.
		/// </value>
		public List<string> Tags { get; set; }

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

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is CatalogMetaData other)
			{
				return String.Equals(CatalogIdentifier, other.CatalogIdentifier, StringComparison.OrdinalIgnoreCase) &&
					   String.Equals(ContentType, other.ContentType, StringComparison.OrdinalIgnoreCase) &&
					   String.Equals(SourceCodeUri, other.SourceCodeUri, StringComparison.OrdinalIgnoreCase) &&
					   String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
					   String.Equals(ShortDescription, other.ShortDescription, StringComparison.OrdinalIgnoreCase) &&
					   String.Equals(DocumentationUrl, other.DocumentationUrl, StringComparison.OrdinalIgnoreCase) &&
					   Owners.SequenceEqual(other.Owners) && // List comparison
					   Tags.SequenceEqual(other.Tags) && // List comparison
					   String.Equals(PathToReadme, other.PathToReadme, StringComparison.OrdinalIgnoreCase) &&
					   String.Equals(PathToImages, other.PathToImages, StringComparison.OrdinalIgnoreCase) &&
					   Equals(Version, other.Version); // Version is an object, so we use Equals
			}
			return false;
		}

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			// Combine the hash codes in chunks and then combine the results.
			int hash1 = HashCode.Combine(
				CatalogIdentifier?.ToLower(),
				ContentType?.ToLower(),
				SourceCodeUri?.ToLower(),
				Name?.ToLower(),
				ShortDescription?.ToLower()
			);

			int hash2 = HashCode.Combine(
				DocumentationUrl?.ToLower(),
				PathToReadme?.ToLower(),
				PathToImages?.ToLower(),
				Owners != null ? String.Join(",", Owners).ToLower() : String.Empty
			);

			int hash3 = HashCode.Combine(
				Tags != null ? String.Join(",", Tags).ToLower() : String.Empty,
				Version
			);

			// Combine the intermediate hashes into a final hash.
			return HashCode.Combine(hash1, hash2, hash3);
		}

		/// <summary>
		/// Whether this is a pre-release or a full release. This is automatically decided from the Version and artifact content.
		/// </summary>
		public bool IsPreRelease()
		{
			// Might need to check for protocols how to handle this. "_BX" probably added to versions?
			// Should we allow a force set/override?

			if (!ArtifactHadBuildNumber)
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

		/// <summary>
		/// Searches for and applies metadata from the closest catalog YAML file in the specified directory.
		/// </summary>
		/// <param name="fs">The file system interface to use for file operations.</param>
		/// <param name="startPath">The starting directory or file path for the search.</param>
		/// <returns>True if a catalog YAML file is found and applied, otherwise false.</returns>
		public bool SearchAndApplyCatalogYaml(IFileSystem fs, string startPath)
		{
			string foundYaml;
			if (fs.Directory.IsDirectory(startPath))
			{
				foundYaml = RecursiveFindClosestCatalogYaml(fs, startPath, 5);
			}
			else if (startPath.EndsWith(".yml"))
			{
				foundYaml = startPath;
			}
			else
			{
				var directory = fs.File.GetParentDirectory(startPath);
				foundYaml = RecursiveFindClosestCatalogYaml(fs, directory, 5);
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
			if (p != null)
			{
				if (!String.IsNullOrWhiteSpace(p.Id)) CatalogIdentifier = p.Id;
				if (!String.IsNullOrWhiteSpace(p.Type)) ContentType = p.Type;
				if (!String.IsNullOrWhiteSpace(p.Title)) Name = p.Title;

				if (!String.IsNullOrWhiteSpace(p.Short_description)) ShortDescription = p.Short_description;
				if (!String.IsNullOrWhiteSpace(p.Source_code_url)) SourceCodeUri = p.Source_code_url;

				p.Owners?.ForEach(o => { Owners.Add(new CatalogOwner { Name = o.Name, Email = o.Email, Url = o.Url }); });
				p.Tags?.AddRange(Tags);
			}

			return true;
		}

		/// <summary>
		/// Searches for and applies metadata from the closest README.md file in the specified directory.
		/// Also searches for the closest "images" folder.
		/// </summary>
		/// <param name="fs">The file system interface to use for file operations.</param>
		/// <param name="startPath">The starting directory or file path for the search.</param>
		/// <returns>True if a README.md file is found and applied, otherwise false.</returns>
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

			var directoryForImages = fs.File.GetParentDirectory(PathToReadme);
			string foundImages = RecursiveFindClosestImages(fs, directoryForImages, 5);

			PathToImages = foundImages;
			return true;
		}

		/// <summary>
		/// Asynchronously creates a zip file containing catalog metadata, README.md, and images folder if available.
		/// </summary>
		/// <returns>A byte array representing the zip file.</returns>
		public async Task<byte[]> ToCatalogZipAsync(IFileSystem fs, ISerializer serializer)
		{
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
						await streamWriter.WriteAsync(yaml).ConfigureAwait(false);
						await streamWriter.FlushAsync().ConfigureAwait(false); // Ensure everything is written
					}

					// Add README.md
					if (PathToReadme != null && fs.File.Exists(PathToReadme))
					{
						var readmeEntry = archive.CreateEntry("README.md");
						using (var entryStream = readmeEntry.Open())
						{
							var readmeContent = fs.File.ReadAllText(PathToReadme); // Get the file content as a string
							using (var streamWriter = new StreamWriter(entryStream))
							{
								await streamWriter.WriteAsync(readmeContent).ConfigureAwait(false);
								await streamWriter.FlushAsync().ConfigureAwait(false); // Ensure all content is written
							}
						}
					}

					// Add Images folder
					if (PathToImages != null && fs.Directory.Exists(PathToImages))
					{
						var imageFiles = fs.Directory.GetFiles(PathToImages);
						foreach (var imageFile in imageFiles)
						{
							var imageEntry = archive.CreateEntry($"Images/{fs.Path.GetFileName(imageFile)}");
							using (var entryStream = imageEntry.Open())
							{
								var readmeContent = fs.File.ReadAllText(imageFile); // Get the file content as a string
								using (var streamWriter = new StreamWriter(entryStream))
								{
									await streamWriter.WriteAsync(readmeContent).ConfigureAwait(false);
									await streamWriter.FlushAsync().ConfigureAwait(false); // Ensure all content is written
								}
							}
						}
					}
				}

				// Convert zip file to byte array
				return memoryStream.ToArray();
			}
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
				if (fileName != null && (fileName.Equals("manifest.yml", StringComparison.InvariantCultureIgnoreCase) || fileName.Equals("catalog.yml")))
				{
					return file;
				}
			}

			var parent = fs.Directory.GetParentDirectory(directory);
			if (String.IsNullOrWhiteSpace(parent)) return null;
			return RecursiveFindClosestCatalogYaml(fs, parent, maxRecurse);
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
				if (fileName != null && fileName.Equals("images", StringComparison.InvariantCultureIgnoreCase))
				{
					return subdir;
				}
			}

			var parent = fs.Directory.GetParentDirectory(directory);
			if (String.IsNullOrWhiteSpace(parent)) return null;
			return RecursiveFindClosestImages(fs, parent, maxRecurse);
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
				if (fileName != null && fileName.Equals("readme.md", StringComparison.InvariantCultureIgnoreCase))
				{
					return file;
				}
			}

			var parent = fs.Directory.GetParentDirectory(directory);
			if (String.IsNullOrWhiteSpace(parent)) return null;
			return RecursiveFindClosestReadmeMd(fs, parent, maxRecurse);
		}
	}

	/// <summary>
	/// Represents the owner of the catalog.
	/// </summary>
	public class CatalogOwner
	{
		/// <summary>
		/// Gets or sets the email of the catalog owner.
		/// </summary>
		/// <value>
		/// A string representing the owner's email address.
		/// </value>
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the name of the catalog owner.
		/// </summary>
		/// <value>
		/// A string representing the owner's name.
		/// </value>
		public string Name { get; set; }

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
		/// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
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
		/// Initializes a new instance of the <see cref="CatalogVersionMetaData"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor sets default values for the version metadata.
		/// The version value is initialized as an empty string, and the description is set to "No Description."
		/// </remarks>
		public CatalogVersionMetaData()
		{
			// Required values for new version registrations.
			Value = String.Empty;
			VersionDescription = "No Description.";
		}

		/// <summary>
		/// The Branch/Range/Category this version belongs to. Defaults to "main"
		/// </summary>
		public string Branch { get; set; } = "main";

		/// <summary>
		/// Gets or sets the email address of the committer.
		/// </summary>
		/// <value>
		/// A string representing the committer's email address.
		/// </value>
		public string CommitterMail { get; set; }

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
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
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