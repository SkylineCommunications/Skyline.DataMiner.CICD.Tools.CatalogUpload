namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Logging;

	using Newtonsoft.Json;

	using Skyline.DataMiner.CICD.FileSystem;

	/// <summary>
	/// Allows Uploading an artifact to the Catalog using one of the below in order of priority:
	///  <para>- provided key in upload argument (unix/win)</para>
	///  <para>- key stored as an Environment Variable called "dmcatalogtoken". (unix/win)</para>
	///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "dmcatalogtoken_encrypted" (windows only)</para>
	/// </summary>
	public class CatalogArtifact
	{
		private readonly ILogger _logger;
		private readonly CatalogMetaData metaData;

		/// <summary>
		/// Creates an instance of <see cref="CatalogArtifact"/>.
		/// It searches for an optional dmCatalogToken in the "dmcatalogtoken" or "dmcatalogtoken_encrypted" Environment Variable.
		/// </summary>
		/// <param name="pathToArtifact">Path to the ".dmapp" or ".dmprotocol" file.</param>
		/// <param name="service">An instance of <see cref="ICatalogService"/> used for communication.</param>
		/// <param name="fileSystem">An instance of <see cref="IFileSystem"/> to access the filesystem. e.g. Skyline.DataMiner.CICD.FileSystem.Instance.</param>
		/// <param name="logger">An instance of <see cref="ILogger"/> that will hold error, debug and other information.</param>
		/// <param name="metaData">Contains package metadata.</param>
		public CatalogArtifact(string pathToArtifact, ICatalogService service, IFileSystem fileSystem, ILogger logger, CatalogMetaData metaData)
		{
			this.metaData = metaData;
			_logger = logger;
			Fs = fileSystem;
			cancellationTokenSource = new CancellationTokenSource();
			catalogService = service;
			PathToArtifact = pathToArtifact;
			TryFindEnvironmentKey();
		}

		/// <summary>
		/// Creates an instance of <see cref="CatalogArtifact"/> using a default HttpCatalogService with a new HttpClient for communication.
		/// It searches for an optional dmCatalogToken in the "dmcatalogtoken" or "dmcatalogtoken_encrypted" Environment Variable for authentication.
		/// WARNING: when wishing to upload several Artifacts it's recommended to use the CatalogArtifact(string pathToArtifact, ICatalogService service, IFileSystem fileSystem, ILogger logger).
		/// </summary>
		/// <param name="pathToArtifact">Path to the ".dmapp" or ".dmprotocol" file.</param>
		/// <param name="logger">An instance of <see cref="ILogger"/> that will hold error, debug and other information.</param>
		/// <param name="metaData">Contains package metadata.</param>
		public CatalogArtifact(string pathToArtifact, ILogger logger, CatalogMetaData metaData) : this(pathToArtifact, CatalogServiceFactory.CreateWithHttp(new System.Net.Http.HttpClient(), logger), FileSystem.Instance, logger, metaData)
		{
		}

		/// <summary>
		/// Path to the ".dmapp" or ".dmprotocol" file.
		/// </summary>
		public string PathToArtifact { get; private set; }

		private CancellationTokenSource cancellationTokenSource { get; set; }

		private ICatalogService catalogService { get; set; }

		private IFileSystem Fs { get; set; }

		private string keyFromEnv { get; set; }

		/// <summary>
		/// Cancels an ongoing upload. Create a new CatalogArtifact to attempt a new upload.
		/// </summary>
		public void CancelUpload()
		{
			_logger.LogDebug($"Upload cancellation requested for {PathToArtifact}");
			cancellationTokenSource.Cancel();
		}

		/// <summary>
		/// Uploads to the private catalog using the provided dmCatalogToken.
		/// </summary>
		/// <param name="dmCatalogToken">A provided token for the agent or organization as defined in https://admin.dataminer.services/.</param>
		/// <returns>If the upload was successful or not.</returns>
		public async Task<ArtifactUploadResult> UploadAsync(string dmCatalogToken)
		{
			_logger.LogDebug($"Uploading {PathToArtifact}...");

			byte[] packageData = Fs.File.ReadAllBytes(PathToArtifact);
			var result = await catalogService.ArtifactUploadAsync(packageData, dmCatalogToken, metaData, cancellationTokenSource.Token).ConfigureAwait(false);
			_logger.LogDebug($"Finished Uploading {PathToArtifact}");

			_logger.LogInformation(JsonConvert.SerializeObject(result));
			return result;
		}

		/// <summary>
		/// Uploads to the private catalog using the dmcatalogtoken or dmcatalogtoken environment variable as the token.
		/// </summary>
		/// <returns>If the upload was successful or not.</returns>
		/// <exception cref="InvalidOperationException">Uploading failed.</exception>
		/// <exception cref="UnauthorizedAccessException">Uploading failed due to invalid Token.</exception>
		public async Task<ArtifactUploadResult> UploadAsync()
		{
			if (String.IsNullOrWhiteSpace(keyFromEnv))
			{
				throw new InvalidOperationException("Uploading failed, missing token in environment variable dmcatalogtoken or dmcatalogtoken_encrypted.");
			}

			_logger.LogDebug($"Attempting upload with Environment Variable as token for artifact: {PathToArtifact}...");
			return await UploadAsync(keyFromEnv).ConfigureAwait(false);
		}

		/// <summary>
		///  Attempts to find the necessary API key in Environment Variables. In order of priority:
		///  <para>- key stored as an Environment Variable called "dmcatalogtoken". (unix/win)</para>
		///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "dmcatalogtoken_encrypted" (windows only)</para>
		/// </summary>
		private void TryFindEnvironmentKey()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				try
				{
					var encryptedKey = WinEncryptedKeys.Lib.Keys.RetrieveKey("dmcatalogtoken_encrypted");
					if (encryptedKey != null)
					{
						string keyFromWinEncryptedKeys = new System.Net.NetworkCredential(string.Empty, encryptedKey).Password;

						if (!String.IsNullOrWhiteSpace(keyFromWinEncryptedKeys))
						{
							_logger.LogDebug("OK: Found token in Env Variable: 'dmcatalogtoken_encrypted' created by WinEncryptedKeys.");
							keyFromEnv = keyFromWinEncryptedKeys;
						}
					}
				}
				catch (InvalidOperationException)
				{
					// Gobble up, no key means we try the next thing.
				}
			}

			//var config = new ConfigurationBuilder()
			//	.AddUserSecrets<CatalogArtifact>()
			//	.Build();
			//string keyFromEnvironment = config["dmcatalogtoken"];

			string keyFromEnvironment = Environment.GetEnvironmentVariable("dmcatalogtoken");

			if (!String.IsNullOrWhiteSpace(keyFromEnvironment))
			{
				if (!String.IsNullOrWhiteSpace(keyFromEnv))
				{
					_logger.LogDebug("OK: Overriding 'dmcatalogtoken_encrypted' with found token in Env Variable: 'dmcatalogtoken'.");
				}
				else
				{
					_logger.LogDebug("OK: Found token in Env Variable: 'dmcatalogtoken'.");
				}

				keyFromEnv = keyFromEnvironment;
			}
		}
	}
}