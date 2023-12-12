namespace Skyline.DataMiner.CICD.Tools.CatalogUpload
{
	using System.CommandLine;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Serilog;

	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;

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
			var pathToArtifact = new Option<string>(
				name: "--pathToArtifact",
				description: "The path to a .dmapp or .dmprotocol file.")
			{
				IsRequired = true
			};

			var dmCatalogToken = new Option<string>(
			name: "--dmCatalogToken",
			description: "The key to upload to the catalog as defined in admin.dataminer.services. This is optional if the key can also be provided using the 'dmcatalogtoken' environment variable (unix/win) or using 'dmcatalogtoken_encrypted' configured with Skyline.DataMiner.CICD.Tools.WinEncryptedKeys (windows).")
			{
				IsRequired = false
			};

			var isDebug = new Option<bool>(
			name: "--debug",
			description: "Indicates the tool should write out debug logging.")
			{
				IsRequired = false,
			};

			var rootCommand = new RootCommand("Uploads artifacts to the artifact cloud. (The default upload has no additional registration and no visibility on the catalog. Use the returned Artifact ID for deployment or download.)");
			rootCommand.AddGlobalOption(pathToArtifact);
			rootCommand.AddGlobalOption(dmCatalogToken);
			rootCommand.AddGlobalOption(isDebug);

			// subcommand "WithRegistration  with the required sourcecode then and optional other arguments.
			var registrationIdentifier = new Option<string>(
			name: "--sourcecode",
			description: "A Uri for the globally unique location of your sourcecode. This is used as a unique identifier. e.g. https://github.com/SkylineCommunications/MyTestRepo")
			{
				IsRequired = true,
			};

			var overrideVersion = new Option<string>(
			name: "--artifactVersion",
			description: "Optional but recommended, include a different version than the internal package version to register your package under (this can be a pre-release version). e.g. '1.0.1', '1.0.1-prerelease1', '1.0.0.1'")
			{
				IsRequired = false,
			};

			var branch = new Option<string>(
			name: "--branch",
			description: "Specifies what branch does this version of your package belong to, e.g. 'main', '1.0.0.X', '1.0.X', 'dev/somefeature', etc. Defaults to 'main' when not provided. "
			{
				IsRequired = false,
			};

			var committerMail = new Option<string>(
			name: "--authorMail",
			description: "Optionally include the e-mail of the uploader.")
			{
				IsRequired = false,
			};

			var releaseUri = new Option<string>(
			name: "--releaseNotes",
			description: "Optionally include a URL to the release notes. e.g. https://github.com/SkylineCommunications/MyTestRepo/releases/tag/1.0.3")
			{
				IsRequired = false,
			};

			var withRegistrationCommand = new Command("WithRegistration", "Uploads artifacts to become visible in the Skyline DataMiner catalog (https://catalog.dataminer.services")
			{
				registrationIdentifier,
				overrideVersion,
				branch,
				committerMail,
				releaseUri
			};

			rootCommand.SetHandler(Process, pathToArtifact, dmCatalogToken, isDebug);
			withRegistrationCommand.SetHandler(ProcessWithRegistration, pathToArtifact, dmCatalogToken, isDebug, registrationIdentifier, overrideVersion, branch, committerMail, releaseUri);

			rootCommand.Add(withRegistrationCommand);
			return await rootCommand.InvokeAsync(args);
		}

		private static async Task<int> Process(string pathToArtifact, string dmCatalogToken, bool isDebug)
		{
			return await ProcessWithRegistration(pathToArtifact, dmCatalogToken, isDebug, null, null, null, null, null);
		}

		private static async Task<int> ProcessWithRegistrationAsync(string pathToArtifact, string dmCatalogToken, bool isDebug, string registrationIdentifier, string overrideVersion, string branch, string committerMail, string releaseUri)
		{
			LoggerConfiguration logConfig;

			if (isDebug)
			{
				logConfig = new LoggerConfiguration().WriteTo.Console(Serilog.Events.LogEventLevel.Information);
			}
			else
			{
				logConfig = new LoggerConfiguration().WriteTo.Console(Serilog.Events.LogEventLevel.Debug);
			}

			var seriLog = logConfig.CreateLogger();

			LoggerFactory loggerFactory = new LoggerFactory();
			loggerFactory.AddSerilog(seriLog);

			var logger = loggerFactory.CreateLogger("Skyline.DataMiner.CICD.Tools.CatalogUpload");

			CatalogMetaData metaData = CatalogMetaData.FromArtifact(pathToArtifact);

			if (registrationIdentifier != null)
			{
				// Registration as a whole is optional. If there is no Identifier provided there will be no registration.
				metaData.Identifier = registrationIdentifier; // Need from user <- optional unique identifier. Usually the path to the sourcecode on github/gitlab/git.

				// These are optional. Only override if not null.
				if (overrideVersion != null) metaData.Version = overrideVersion;
				if (branch != null) metaData.Branch = branch;
				if (committerMail != null) metaData.CommitterMail = committerMail;
				if (releaseUri != null) metaData.ReleaseUri = releaseUri;
			}

			CatalogArtifact artifact = new CatalogArtifact(pathToArtifact, logger, metaData);

			if (string.IsNullOrWhiteSpace(dmCatalogToken))
			{
				await artifact.UploadAsync();
			}
			else
			{
				await artifact.UploadAsync(dmCatalogToken);
			}

			return 0;
		}
	}
}