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

			var dmCatalogKey = new Option<string>(
			name: "--dmCatalogKey",
			description: "The key to upload to the catalog as defined in admin.dataminer.services. This is Optional: the key can also be provided using the 'dmcatalogkey' environment variable (unix/win) or using 'dmcatalogkey_encrypted' configured with Skyline.DataMiner.CICD.Tools.WinEncryptedKeys (windows).")
			{
				IsRequired = false
			};

			var isDebug = new Option<bool>(
			name: "--debug",
			description: "Indicates the tool should write out debug logging.")
			{
				IsRequired = false,
			};

			var rootCommand = new RootCommand("Uploads artifacts to the Skyline DataMiner catalog (https://catalog.dataminer.services)")
			{
				pathToArtifact,
			};

			rootCommand.SetHandler(Process, pathToArtifact, dmCatalogKey, isDebug);

			return await rootCommand.InvokeAsync(args);
		}

		private static async Task<int> Process(string pathToArtifact, string dmCatalogKey, bool isDebug)
		{
			bool success;
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

			CatalogArtifact artifact = new CatalogArtifact(pathToArtifact, logger, metaData);
	
			if (string.IsNullOrWhiteSpace(dmCatalogKey))
			{
				await artifact.UploadAsync();
			}
			else
			{
				await artifact.UploadAsync(dmCatalogKey);
			}

			return 0;
		}
	}
}