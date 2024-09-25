namespace CICD.Tools.CatalogUpload
{
	using System;
	using System.IO.Compression;
	using System.IO;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Serilog;

	using Skyline.DataMiner.CICD.FileSystem;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;
	using Skyline.DataMiner.CICD.Tools.Reporter;

	internal class Uploader
	{
		readonly Microsoft.Extensions.Logging.ILogger logger;
		readonly IFileSystem fs;

		public Uploader(bool isDebug)
		{
			LoggerConfiguration logConfig = new LoggerConfiguration().WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
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

			logger = loggerFactory.CreateLogger("Skyline.DataMiner.CICD.Tools.CatalogUpload");

			fs = FileSystem.Instance;
		}

		public async Task<int> ProcessYmlRegistrationAsync(string dmCatalogToken, string catalogDetailsYml, string readme, string images)
		{
			string devopsMetricsMessage = "Skyline.DataMiner.CICD.Tools.CatalogUpload|only-registration";

			try
			{
				CatalogMetaData metaData = CatalogMetaData.FromCatalogYaml(fs, catalogDetailsYml, readme, images);

				devopsMetricsMessage += $"|Status:OK";
			}
			catch (Exception e)
			{
				devopsMetricsMessage += "|" + "Status:Fail-" + e.Message;
				Console.WriteLine("Exception: " + e);
				return 1;
			}
			finally
			{
				if (!string.IsNullOrWhiteSpace(devopsMetricsMessage))
				{
					try
					{
						DevOpsMetrics devOpsMetrics = new DevOpsMetrics();
						await devOpsMetrics.ReportAsync(devopsMetricsMessage).ConfigureAwait(false);
					}
					catch
					{
						// Fire and forget.
					}
				}
			}

			return 0;
		}

		public async Task<int> ProcessVolatile(string pathToArtifact, string dmCatalogToken)
		{
			string devopsMetricsMessage = "Skyline.DataMiner.CICD.Tools.CatalogUpload|volatile";

			try
			{
				// Order of priority first the content of the artifact. Then the provided yml file. Finally any arguments from the tool.
				CatalogMetaData metaData = CatalogMetaData.FromArtifact(pathToArtifact);
				metaData.SearchAndApplyCatalogYaml(fs, pathToArtifact);

				var artifact = new CatalogArtifact(pathToArtifact, logger, metaData);

				ArtifactUploadResult result;
				if (dmCatalogToken != null)
				{
					result = await artifact.VolatatileUploadAsync(dmCatalogToken).ConfigureAwait(false);
				}
				else
				{
					result = await artifact.VolatatileUploadAsync().ConfigureAwait(false);
				}

				devopsMetricsMessage += $"|Status:OK|ArtifactId:{result.ArtifactId}";
			}
			catch (Exception e)
			{
				devopsMetricsMessage += "|" + "Status:Fail-" + e.Message;
				Console.WriteLine("Exception: " + e);
				return 1;
			}
			finally
			{
				if (!string.IsNullOrWhiteSpace(devopsMetricsMessage))
				{
					try
					{
						DevOpsMetrics devOpsMetrics = new DevOpsMetrics();
						await devOpsMetrics.ReportAsync(devopsMetricsMessage).ConfigureAwait(false);
					}
					catch
					{
						// Fire and forget.
					}
				}
			}

			return 0;
		}

		public async Task<int> ProcessWithRegistrationAsync(string dmCatalogToken, string pathToArtifact, string uriSourceCode, string overrideVersion, string branch, string committerMail, string releaseUri)
		{
			string devopsMetricsMessage = "Skyline.DataMiner.CICD.Tools.CatalogUpload|with-registration";

			try
			{
				// Order of priority first the content of the artifact. Then the provided yml file. Finally any arguments from the tool.
				CatalogMetaData metaData = CatalogMetaData.FromArtifact(pathToArtifact);
				metaData.SearchAndApplyCatalogYaml(fs, pathToArtifact);
				metaData.SearchAndApplyReadMe(fs, pathToArtifact);
				ApplyOptionalArguments(uriSourceCode, overrideVersion, branch, committerMail, releaseUri, metaData);

				var artifact = new CatalogArtifact(pathToArtifact, logger, metaData);

				ArtifactUploadResult result;
				if (dmCatalogToken != null)
				{
					result = await artifact.UploadAndRegisterAsync(dmCatalogToken).ConfigureAwait(false);
				}
				else
				{
					result = await artifact.UploadAndRegisterAsync().ConfigureAwait(false);
				}

				devopsMetricsMessage += $"|Status:OK|ArtifactId:{result.ArtifactId}";
			}
			catch (Exception e)
			{
				devopsMetricsMessage += "|" + "Status:Fail-" + e.Message;
				Console.WriteLine("Exception: " + e);
				return 1;
			}
			finally
			{
				if (!string.IsNullOrWhiteSpace(devopsMetricsMessage))
				{
					try
					{
						DevOpsMetrics devOpsMetrics = new DevOpsMetrics();
						await devOpsMetrics.ReportAsync(devopsMetricsMessage).ConfigureAwait(false);
					}
					catch
					{
						// Fire and forget.
					}
				}
			}

			return 0;
		}

		private static void ApplyOptionalArguments(string uriSourceCode, string overrideVersion, string branch, string committerMail, string releaseUri, CatalogMetaData metaData)
		{
			if (uriSourceCode != null) metaData.SourceCodeUri = uriSourceCode;
			if (overrideVersion != null) metaData.Version.Value = overrideVersion;
			if (branch != null) metaData.Version.Branch = branch;
			if (committerMail != null) metaData.Version.CommitterMail = committerMail;
			if (releaseUri != null) metaData.Version.ReleaseUri = releaseUri;
		}
	}
}
