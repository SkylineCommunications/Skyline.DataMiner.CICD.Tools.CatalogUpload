namespace Skyline.DataMiner.CICD.Tools.CatalogUpload
{
	using System;
	using System.CommandLine;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Serilog;

	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;
	using Skyline.DataMiner.CICD.Tools.Reporter;

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
				name: "--path-to-artifact",
				description: "Path to the application package (.dmapp). Important: does not support protocol packages (.dmprotocol).")
			{
				IsRequired = true
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

			var rootCommand = new RootCommand("Uploads artifacts to the artifact cloud. (The default upload has no additional registration and no visibility on the catalog. Use the returned Artifact ID for deployment or download.)");
			rootCommand.AddGlobalOption(pathToArtifact);
			rootCommand.AddGlobalOption(dmCatalogToken);
			rootCommand.AddGlobalOption(isDebug);

			// subcommand "WithRegistration  with the required sourcecode then and optional other arguments.
			var registrationIdentifier = new Option<string>(
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

			var withRegistrationCommand = new Command("with-registration", "Uploads artifacts to become visible in the Skyline DataMiner catalog (https://catalog.dataminer.services")
			{
				registrationIdentifier,
				overrideVersion,
				branch,
				committerMail,
				releaseUri
			};

			rootCommand.SetHandler(Process, pathToArtifact, dmCatalogToken, isDebug);
			withRegistrationCommand.SetHandler(ProcessWithRegistrationAsync, pathToArtifact, dmCatalogToken, isDebug, registrationIdentifier, overrideVersion, branch, committerMail, releaseUri);

			rootCommand.Add(withRegistrationCommand);
			return await rootCommand.InvokeAsync(args);
		}

		private static async Task<int> Process(string pathToArtifact, string dmCatalogToken, bool isDebug)
		{
			return await ProcessWithRegistrationAsync(pathToArtifact, dmCatalogToken, isDebug, null, null, null, null, null);
		}

		private static async Task<int> ProcessWithRegistrationAsync(string pathToArtifact, string dmCatalogToken, bool isDebug, string registrationIdentifier, string overrideVersion, string branch, string committerMail, string releaseUri)
		{

			if (pathToArtifact.EndsWith(".dmprotocol", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new ArgumentException("protocol packages (.dmprotocol) are currently not supported.");
			}

			// Skyline.DataMiner.CICD.Tools.CatalogUpload|with-registration:https://github.com/SomeRepo|Status:OK"
			// Skyline.DataMiner.CICD.Tools.CatalogUpload|with-registration:https://github.com/SomeRepo|Status:Fail-blabla"
			// Skyline.DataMiner.CICD.Tools.CatalogUpload|volatile|Status:OK"
			// Skyline.DataMiner.CICD.Tools.CatalogUpload|volatile|Status:Fail-blabla"
			string devopsMetricsMessage = "Skyline.DataMiner.CICD.Tools.CatalogUpload";

			try
			{
				LoggerConfiguration logConfig = new LoggerConfiguration().WriteTo.Console();
				if (!isDebug)
				{
					logConfig.MinimumLevel.Information();
				}
				else
				{

					logConfig.MinimumLevel.Debug();
				}

				var seriLog = logConfig.CreateLogger();

				LoggerFactory loggerFactory = new LoggerFactory();
				loggerFactory.AddSerilog(seriLog);

				var logger = loggerFactory.CreateLogger("Skyline.DataMiner.CICD.Tools.CatalogUpload");

				CatalogMetaData metaData = CatalogMetaData.FromArtifact(pathToArtifact);

				if (registrationIdentifier != null)
				{
					devopsMetricsMessage += "|with-registration:" + registrationIdentifier;
					// Registration as a whole is optional. If there is no Identifier provided there will be no registration.
					metaData.Identifier = registrationIdentifier; // Need from user <- optional unique identifier. Usually the path to the sourcecode on github/gitlab/git.

					// These are optional. Only override if not null.
					if (overrideVersion != null) metaData.Version = overrideVersion;
					if (branch != null) metaData.Branch = branch;
					if (committerMail != null) metaData.CommitterMail = committerMail;
					if (releaseUri != null) metaData.ReleaseUri = releaseUri;
				}
				else
				{
					devopsMetricsMessage += "|volatile";
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

				devopsMetricsMessage += "|Status:OK";
			}
			catch (Exception e)
			{
				devopsMetricsMessage += "|" + "Status:Fail-" + e.Message;
				throw;
			}
			finally
			{
				if (!string.IsNullOrWhiteSpace(devopsMetricsMessage))
				{
					try
					{
						DevOpsMetrics devOpsMetrics = new DevOpsMetrics();
						await devOpsMetrics.ReportAsync(devopsMetricsMessage);
					}
					catch
					{
						// Fire and forget.
					}
				}
			}

			return 0;
		}
	}
}