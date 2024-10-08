namespace CICD.Tools.CatalogUpload
{
	using System;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Skyline.DataMiner.CICD.FileSystem;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;
	using Skyline.DataMiner.CICD.Tools.Reporter;

	/// <summary>
	/// Handles the uploading and registration of catalog artifacts to a DataMiner catalog.
	/// </summary>
	public class Uploader
	{
		private readonly IFileSystem fs;
		private readonly Microsoft.Extensions.Logging.ILogger logger;
		private readonly ICatalogMetaDataFactory catalogMetaDataFactory;
		private readonly ICatalogService service;

		/// <summary>
		/// Initializes a new instance of the <see cref="Uploader"/> class.
		/// </summary>
		/// <param name="fileSystem">The file system interface used for accessing files and directories, providing functionality for reading and writing files.</param>
		/// <param name="logger">The logger instance used for logging informational, warning, and error messages throughout the upload process.</param>
		/// <param name="service">The catalog service used to interact with the catalog, such as uploading artifacts and retrieving metadata.</param>
		/// <param name="catalogMetaDataFactory">Factory responsible for creating instances of catalog metadata, used during artifact registration.</param>
		public Uploader(IFileSystem fileSystem, Microsoft.Extensions.Logging.ILogger logger, ICatalogService service, ICatalogMetaDataFactory catalogMetaDataFactory)
		{
			this.service = service;
			fs = fileSystem;
			this.catalogMetaDataFactory = catalogMetaDataFactory;
			this.logger = logger;
		}

		/// <summary>
		/// Processes an artifact by uploading it to a volatile catalog without registration.
		/// </summary>
		/// <param name="pathToArtifact">The path to the artifact being uploaded.</param>
		/// <param name="dmCatalogToken">The token used for authentication during upload.</param>
		/// <returns>An integer result indicating success (0) or failure (1).</returns>
		public async Task<int> ProcessVolatile(string pathToArtifact, string dmCatalogToken)
		{
			string devopsMetricsMessage = "Skyline.DataMiner.CICD.Tools.CatalogUpload|volatile";

			try
			{
				// Order of priority first the content of the artifact. Then the provided yml file. Finally any arguments from the tool.
				CatalogMetaData metaData = catalogMetaDataFactory.FromArtifact(pathToArtifact);
				metaData.SearchAndApplyCatalogYaml(fs, pathToArtifact);

				var artifact = new CatalogArtifact(pathToArtifact, service, fs, logger, metaData);

				ArtifactUploadResult result;
				if (dmCatalogToken != null)
				{
					logger.LogDebug("Uploading artifact to the volatile catalog using provided key...");
					result = await artifact.VolatileUploadAsync(dmCatalogToken).ConfigureAwait(false);
				}
				else
				{
					logger.LogDebug("Uploading artifact to the volatile catalog using environment key...");
					result = await artifact.VolatatileUploadAsync().ConfigureAwait(false);
				}

				logger.LogDebug($"Artifact Uploaded with {result.ArtifactId}.");
				devopsMetricsMessage += $"|Status:OK|ArtifactId:{result.ArtifactId}";
			}
			catch (Exception e)
			{
				devopsMetricsMessage += "|" + "Status:Fail-" + e.Message;
				logger.LogError(e, "An error occurred during processing");
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

			logger.LogDebug("Process completed successfully.");
			return 0;
		}

		/// <summary>
		/// Processes an artifact by uploading and registering it in the catalog.
		/// </summary>
		/// <param name="dmCatalogToken">The token used for authentication during registration and upload.</param>
		/// <param name="pathToArtifact">The path to the artifact being uploaded and registered.</param>
		/// <param name="optionalArguments"> A set of optional arguments that provide additional information for the registration process.</param>
		/// <returns>An integer result indicating success (0) or failure (1).</returns>
		public async Task<int> ProcessWithRegistrationAsync(string dmCatalogToken, string pathToArtifact, OptionalRegistrationArguments optionalArguments)
		{
			string devopsMetricsMessage = "Skyline.DataMiner.CICD.Tools.CatalogUpload|with-registration";

			try
			{
				// Order of priority first the content of the artifact. Then the provided yml file. Finally any arguments from the tool.
				CatalogMetaData metaData = catalogMetaDataFactory.FromArtifact(pathToArtifact);
				metaData.SearchAndApplyCatalogYaml(fs, pathToArtifact);
				metaData.SearchAndApplyReadMe(fs, pathToArtifact);
				ApplyOptionalArguments(optionalArguments, metaData);

				var artifact = new CatalogArtifact(pathToArtifact, service, fs, logger, metaData);

				ArtifactUploadResult result;
				if (dmCatalogToken != null)
				{
					logger.LogDebug("Registering and uploading artifact to the catalog using provided key...");
					result = await artifact.UploadAndRegisterAsync(dmCatalogToken).ConfigureAwait(false);
				}
				else
				{
					logger.LogDebug("Registering and uploading artifact to the catalog using environment key...");
					result = await artifact.UploadAndRegisterAsync().ConfigureAwait(false);
				}

				logger.LogDebug($"Artifact Uploaded with {result.ArtifactId}.");
				devopsMetricsMessage += $"|Status:OK|ArtifactId:{result.ArtifactId}";
			}
			catch (Exception e)
			{
				devopsMetricsMessage += "|" + "Status:Fail-" + e.Message;
				logger.LogError(e, "An error occurred during processing");
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

			logger.LogDebug("Process completed successfully.");
			return 0;
		}

		/// <summary>
		/// Processes and registers an artifact in the catalog using catalog details from a YAML file.
		/// </summary>
		/// <param name="dmCatalogToken">The token used for authentication during registration.</param>
		/// <param name="catalogDetailsYml">The path to the catalog details YAML file.</param>
		/// <param name="readme">Optional. The path to the README file.</param>
		/// <param name="images">Optional. The path to the images directory.</param>
		/// <returns>An integer result indicating success (0) or failure (1).</returns>
		public async Task<int> ProcessYmlRegistrationAsync(string dmCatalogToken, string catalogDetailsYml, string readme, string images)
		{
			string devopsMetricsMessage = "Skyline.DataMiner.CICD.Tools.CatalogUpload|only-registration";

			try
			{
				CatalogMetaData metaData = catalogMetaDataFactory.FromCatalogYaml(fs, catalogDetailsYml, readme, images);
				CatalogArtifact artifact = new CatalogArtifact(service, fs, logger, metaData);

				ArtifactUploadResult result;
				if (dmCatalogToken != null)
				{
					logger.LogDebug("Registering artifact to the catalog using provided key...");
					result = await artifact.RegisterAsync(dmCatalogToken).ConfigureAwait(false);
				}
				else
				{
					logger.LogDebug("Registering artifact to the catalog using environment key...");
					result = await artifact.RegisterAsync().ConfigureAwait(false);
				}

				logger.LogDebug($"Artifact Registered with {result.ArtifactId}.");
				devopsMetricsMessage += $"|Status:OK";
			}
			catch (Exception e)
			{
				devopsMetricsMessage += "|" + "Status:Fail-" + e.Message;
				logger.LogError(e, "An error occurred during registration");
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

			logger.LogDebug("Process completed successfully.");
			return 0;
		}

		/// <summary>
		/// Applies optional arguments such as version, branch, and source code URI to the catalog metadata.
		/// </summary>
		/// <param name="metaData">The catalog metadata instance to apply the changes to.</param>
		/// <exception cref="ArgumentNullException">Thrown when the metadata is null.</exception>
		private void ApplyOptionalArguments(OptionalRegistrationArguments optionalArguments, CatalogMetaData metaData)
		{
			if (metaData == null)
			{
				throw new ArgumentNullException(nameof(metaData), "Metadata cannot be null.");
			}

			if (optionalArguments.UriSourceCode != null)
			{
				logger.LogDebug($"Overriding SourceCodeUri from '{metaData.SourceCodeUri}' to '{optionalArguments.UriSourceCode}'");
				metaData.SourceCodeUri = optionalArguments.UriSourceCode;
			}

			if (optionalArguments.OverrideVersion != null)
			{
				logger.LogDebug($"Overriding Version from '{metaData.Version.Value}' to '{optionalArguments.OverrideVersion}'");
				metaData.Version.Value = optionalArguments.OverrideVersion;
			}

			if (optionalArguments.Branch != null)
			{
				logger.LogDebug($"Overriding Branch from '{metaData.Version.Branch}' to '{optionalArguments.Branch}'");
				metaData.Version.Branch = optionalArguments.Branch;
			}

			if (optionalArguments.CommitterMail != null)
			{
				logger.LogDebug($"Overriding CommitterMail from '{metaData.Version.CommitterMail}' to '{optionalArguments.CommitterMail}'");
				metaData.Version.CommitterMail = optionalArguments.CommitterMail;
			}

			if (optionalArguments.ReleaseUri != null)
			{
				logger.LogDebug($"Overriding ReleaseUri from '{metaData.Version.ReleaseUri}' to '{optionalArguments.ReleaseUri}'");
				metaData.Version.ReleaseUri = optionalArguments.ReleaseUri;
			}


			if (optionalArguments.CatalogIdentifier != null)
			{
				logger.LogDebug($"Overriding CatalogIdentifier from '{metaData.CatalogIdentifier}' to '{optionalArguments.CatalogIdentifier}'");
				metaData.CatalogIdentifier = optionalArguments.CatalogIdentifier;
			}
		}
	}
}