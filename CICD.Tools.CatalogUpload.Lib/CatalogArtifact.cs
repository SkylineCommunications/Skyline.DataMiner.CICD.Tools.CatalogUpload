namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
	using System;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	using Newtonsoft.Json;

	using Skyline.DataMiner.CICD.FileSystem;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.HttpArtifactUploadModels;

	/// <summary>
	/// Allows Uploading an artifact to the Catalog using one of the below in order of priority:
	///  <para>- provided key in upload argument (unix/win)</para>
	///  <para>- key stored as an Environment Variable called "dmcatalogkey". (unix/win)</para>
	///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "dmcatalogkey_encrypted" (windows only)</para>
	/// </summary>
	public class CatalogArtifact
	{
		private readonly ILogger _logger;
		private readonly CatalogMetaData metaData;

		/// <summary>
		/// Creates an instance of <see cref="CatalogArtifact"/>.
		/// It searches for an optional dmcatalogkey in the "dmcatalogkey" or "dmcatalogkey_encrypted" Environment Variable.
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
		/// It searches for an optional dmcatalogkey in the "dmcatalogkey" or "dmcatalogkey_encrypted" Environment Variable for authentication.
		/// WARNING: when wishing to upload several Artifacts it's recommended to use the CatalogArtifact(string pathToArtifact, ICatalogService service, IFileSystem fileSystem, ILogger logger).
		/// </summary>
		/// <param name="pathToArtifact">Path to the ".dmapp" or ".dmprotocol" file.</param>
		/// <param name="logger">An instance of <see cref="ILogger"/> that will hold error, debug and other information.</param>
		/// <param name="metaData">Contains package metadata.</param>
		public CatalogArtifact(string pathToArtifact, ILogger logger, CatalogMetaData metaData) : this(pathToArtifact, new HttpCatalogService(new System.Net.Http.HttpClient(), logger), FileSystem.Instance, logger, metaData)
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
		/// Uploads to the private catalog using the provided dmcatalogkey.
		/// </summary>
		/// <param name="dmcatalogkey">A provided token for the agent or organization as defined in https://admin.dataminer.services/.</param>
		/// <returns>If the upload was successful or not.</returns>
		public async Task<ArtifactModel> UploadAsync(string dmcatalogkey)
		{
			_logger.LogDebug($"Uploading {PathToArtifact}...");

			byte[] packageData = Fs.File.ReadAllBytes(PathToArtifact);
			var result = await catalogService.ArtifactUploadAsync(packageData, dmcatalogkey, metaData, cancellationTokenSource.Token).ConfigureAwait(false);
			_logger.LogDebug($"Finished Uploading {PathToArtifact}");

			_logger.LogInformation(JsonConvert.SerializeObject(result));
			return result;
		}

		/// <summary>
		/// Uploads to the private catalog using the dmcatalogkey or dmcatalogkey_encrypted environment variable as the token.
		/// </summary>
		/// <returns>If the upload was successful or not.</returns>
		/// <exception cref="InvalidOperationException">Uploading failed.</exception>
		/// <exception cref="UnauthorizedAccessException">Uploading failed due to invalid Token.</exception>
		public async Task<ArtifactModel> UploadAsync()
		{
			if (String.IsNullOrWhiteSpace(keyFromEnv))
			{
				throw new InvalidOperationException("Uploading failed, missing token in environment variable dmcatalogkey or dmcatalogkey_encrypted.");
			}

			_logger.LogDebug($"Attempting upload with Environment Variable as token for artifact: {PathToArtifact}...");
			return await UploadAsync(keyFromEnv).ConfigureAwait(false);
		}

		/// <summary>
		///  Attempts to find the necessary API key in Environment Variables. In order of priority:
		///  <para>- key stored as an Environment Variable called "dmcatalogkey". (unix/win)</para>
		///  <para>- key configured using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys called "dmcatalogkey_encrypted" (windows only)</para>
		/// </summary>
		private void TryFindEnvironmentKey()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var encryptedKey = WinEncryptedKeys.Lib.Keys.RetrieveKey("dmcatalogkey_encrypted");
				if (encryptedKey != null)
				{
					string keyFromWinEncryptedKeys = encryptedKey.ToString();

					if (!String.IsNullOrWhiteSpace(keyFromWinEncryptedKeys))
					{
						_logger.LogDebug("OK: Found token in Env Variable: 'dmcatalogkey_encrypted' created by WinEncryptedKeys.");
						keyFromEnv = keyFromWinEncryptedKeys;
					}
				}
			}

			var config = new ConfigurationBuilder()
				.AddUserSecrets<CatalogArtifact>()
				.Build();
			string keyFromEnvironment = config["dmcatalogkey"];

			if (!String.IsNullOrWhiteSpace(keyFromEnvironment))
			{
				if (!String.IsNullOrWhiteSpace(keyFromEnv))
				{
					_logger.LogDebug("OK: Overriding 'dmcatalogkey_encrypted' with found token in Env Variable: 'dmcatalogkey'.");
				}
				else
				{
					_logger.LogDebug("OK: Found token in Env Variable: 'dmcatalogkey'.");
				}

				keyFromEnv = keyFromEnvironment;
			}
		}
	}
}