namespace Skyline.DataMiner.CICD.Tools.CatalogUpload
{
	using System;
	using System.CommandLine;
	using System.Threading.Tasks;
	using System.Xml.XPath;

	using CICD.Tools.CatalogUpload;

	using global::CICD.Tools.CatalogUpload;

	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;

	using Serilog;
	using Serilog.Core;

	using Skyline.DataMiner.CICD.FileSystem;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;
	using Skyline.DataMiner.CICD.Tools.Reporter;

	using static System.Net.Mime.MediaTypeNames;

	/// <summary>
	/// Uploads artifacts to the Skyline DataMiner catalog (https://catalog.dataminer.services).
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Code that will be called when running the tool.
		/// </summary>
		/// <param name="args">Extra arguments.</param>
		/// <returns>0 if successful.</returns>
		public static async Task<int> Main(string[] args)
		{
			var pathToArtifactRequired = new Option<string>(
				name: "--path-to-artifact",
				description: "Path to the application package (.dmapp).")
			{
				IsRequired = true
			};

			var pathToArtifactOptional = new Option<string>(
			name: "--path-to-artifact",
			description: "Path to the application package (.dmapp).")
			{
				IsRequired = false
			};


			var dmCatalogToken = new Option<string>(
			name: "--dm-catalog-token",
			description: "The key to upload to the catalog as defined in admin.dataminer.services. This is optional if the key can also be provided using the 'DATAMINER_CATALOG_TOKEN' environment variable (unix/win) or using 'DATAMINER_CATALOG_TOKEN_ENCRYPTED' configured with Skyline.DataMiner.CICD.Tools.WinEncryptedKeys (windows).")
			{
				IsRequired = false
			};

			var isDebug = new Option<bool>(
			name: "--debug",
			description: "Indicates the tool should write out debug logging.")
			{
				IsRequired = false,
			};


			// subcommand "WithRegistration  with the required sourcecode then and optional other arguments.
			var uriSourceCode = new Option<string>(
			name: "--uri-sourcecode",
			description: "A Uri for the globally unique location of your sourcecode (not your local workspace). This is used as a unique identifier for registration. e.g. https://github.com/SkylineCommunications/MyTestRepo")
			{
				IsRequired = true,
			};

			var overrideVersion = new Option<string>(
			name: "--artifact-version",
			description: "Optional but recommended, include a different version than the internal package version to register your package under (this can be a pre-release version). e.g. '1.0.1', '1.0.1-prerelease1', '1.0.0.1'")
			{
				IsRequired = false,
			};

			var branch = new Option<string>(
			name: "--branch",
			description: "Specifies what branch does this version of your package belong to, e.g. 'main', '1.0.0.X', '1.0.X', 'dev/somefeature', etc. Defaults to 'main' when not provided. ")
			{
				IsRequired = false,
			};

			var committerMail = new Option<string>(
			name: "--author-mail",
			description: "Optionally include the e-mail of the uploader.")
			{
				IsRequired = false,
			};

			var releaseUri = new Option<string>(
			name: "--release-notes",
			description: "Optionally include a URL to the release notes. e.g. https://github.com/SkylineCommunications/MyTestRepo/releases/tag/1.0.3")
			{
				IsRequired = false,
			};

			var catalogDetailsYml = new Option<string>(
			name: "--path-to-catalog-yml",
			description: "Path to a yml file containing catalog details as described here https://docs.dataminer.services/user-guide/Cloud_Platform/Catalog/Register_Catalog_Item.html#manifest-file")
			{
				IsRequired = true,
			};

			var readme = new Option<string>(
			name: "--path-to-readme",
			description: "Path to a readme file written in markdown.")
			{
				IsRequired = true,
			};

			var images = new Option<string>(
			name: "--path-to-images",
			description: "Path to a folder with images used in the readme file.")
			{
				IsRequired = true,
			};

			// dataminer-catalog-upload
			var rootCommand = new RootCommand("Uploads artifacts or their registration info to the artifact storage for the DataMiner Catalog and Cloud Connected Systems. (The default upload has no additional registration and no visibility on the catalog. Use the returned Artifact ID for deployment or download.)");
			rootCommand.AddOption(pathToArtifactRequired); // No longer global due to onlyRegistration option
			rootCommand.AddGlobalOption(dmCatalogToken);
			rootCommand.AddGlobalOption(isDebug);

			var withRegistrationCommand = new Command("with-registration", "Uploads artifacts to become visible in the Skyline DataMiner catalog (https://catalog.dataminer.services")
			{
				pathToArtifactOptional,
				uriSourceCode,
				overrideVersion,
				branch,
				committerMail,
				releaseUri
			};

			var withOnlyRegistrationCommand = new Command("with-only-registration", "Uploads only the registration information for a Skyline DataMiner catalog (https://catalog.dataminer.services) item.")
			{
				catalogDetailsYml,
				readme,
				images
			};

			rootCommand.SetHandler(ProcessVolatile, pathToArtifactRequired, dmCatalogToken, isDebug);
			withRegistrationCommand.SetHandler(ProcessWithRegistrationAsync, dmCatalogToken, isDebug, pathToArtifactOptional, uriSourceCode, overrideVersion, branch, committerMail, releaseUri);
			withOnlyRegistrationCommand.SetHandler(ProcessYmlRegistrationAsync, dmCatalogToken, isDebug, catalogDetailsYml, readme, images);

			rootCommand.Add(withOnlyRegistrationCommand);
			rootCommand.Add(withRegistrationCommand);
			return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
		}

		private static async Task<int> ProcessYmlRegistrationAsync(string dmCatalogToken, bool isDebug, string catalogDetailsYml, string readme, string images)
		{
			IFileSystem fs = FileSystem.Instance;
			var catalogMetaDataFactory = new CatalogMetaDataFactory();
			Uploader uploader = new Uploader(fs, isDebug, catalogMetaDataFactory);
			return await uploader.ProcessYmlRegistrationAsync(dmCatalogToken, catalogDetailsYml, readme, images).ConfigureAwait(false);
		}

		private static async Task<int> ProcessVolatile(string pathToArtifact, string dmCatalogToken, bool isDebug)
		{
			IFileSystem fs = FileSystem.Instance;
			var catalogMetaDataFactory = new CatalogMetaDataFactory();
			Uploader uploader = new Uploader(fs, isDebug, catalogMetaDataFactory);
			return await uploader.ProcessVolatile(pathToArtifact, dmCatalogToken).ConfigureAwait(false);
		}

		private static async Task<int> ProcessWithRegistrationAsync(string dmCatalogToken, bool isDebug, string pathToArtifact, string uriSourceCode, string overrideVersion, string branch, string committerMail, string releaseUri)
		{
			IFileSystem fs = FileSystem.Instance;
			var catalogMetaDataFactory = new CatalogMetaDataFactory();
			Uploader uploader = new Uploader(fs, isDebug, catalogMetaDataFactory);
			return await uploader.ProcessWithRegistrationAsync(dmCatalogToken, pathToArtifact, uriSourceCode, overrideVersion, branch, committerMail, releaseUri).ConfigureAwait(false);
		}
	}
}