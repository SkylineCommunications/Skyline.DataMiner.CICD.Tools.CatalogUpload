namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Authentication;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    internal sealed class HttpCatalogService : ICatalogService, IDisposable
    {
        /// <summary>
        /// Artifact information returned from uploading an artifact to the catalog using the non-volatile upload.
        /// </summary>
        private sealed class CatalogUploadResult
        {
            [JsonProperty("catalogId")]
            public string? CatalogId { get; set; }

            [JsonProperty("catalogVersionNumber")]
            public string? CatalogVersionNumber { get; set; }

            [JsonProperty("azureStorageId")]
            public string? AzureStorageId { get; set; }
        }


        private const string RegistrationPath = "api/key-catalog/v2-0/catalogs/register";
        private const string VersionUploadPathEnd = "/register/version";
        private const string VersionUploadPathStart = "api/key-catalog/v2-0/catalogs/";
        private const string VolatileUploadPath = "api/key-artifact-upload/v1-0/private/artifact";
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public HttpCatalogService(HttpClient httpClient, ILogger logger)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<ArtifactUploadResult> RegisterCatalogAsync(byte[] catalogDetailsZip, string key, CancellationToken cancellationToken)
        {
            using var formData = new MultipartFormDataContent();
            formData.Headers.Add("Ocp-Apim-Subscription-Key", key);

            // Add file
            using MemoryStream ms = new MemoryStream(catalogDetailsZip);
            ms.Write(catalogDetailsZip, 0, catalogDetailsZip.Length);
            ms.Position = 0;
            formData.Add(new StreamContent(ms), "file", "catalogDetails.zip");

            _logger.LogDebug($"Uploading catalogDetails.zip ({ms.Length} bytes) to {RegistrationPath}");

            // Make PUT request
            var response = await _httpClient.PutAsync(RegistrationPath, formData, cancellationToken).ConfigureAwait(false);

            // Get the response body
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug($"Response: {response.StatusCode}, Body: {body}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"The registration api returned a {response.StatusCode} response. Body: {body}");

                var returnedResult = JsonConvert.DeserializeObject<CatalogUploadResult>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                return new ArtifactUploadResult() { ArtifactId = returnedResult.AzureStorageId };
            }

            _logger.LogError($"The registration api returned a {response.StatusCode} response. Body: {body}");

            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                throw new AuthenticationException($"The registration api returned a {response.StatusCode} response. Body: {body}");
            }

            throw new InvalidOperationException($"The registration api returned a {response.StatusCode} response. Body: {body}");
        }

        public async Task<ArtifactUploadResult> UploadVersionAsync(byte[] package, string fileName, string key, string catalogId, string version, string description, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(version)) throw new ArgumentNullException(nameof(version));

            string versionUploadPath = $"{VersionUploadPathStart}{catalogId}{VersionUploadPathEnd}";
            using var formData = new MultipartFormDataContent();
            formData.Headers.Add("Ocp-Apim-Subscription-Key", key);

            // Add the package (zip file) to the form data
            using MemoryStream ms = new MemoryStream(package);
            ms.Position = 0; // Reset the stream position after writing

            // Set up StreamContent with correct headers for the file
            var fileContent = new StreamContent(ms);

            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = "\"" + fileName + "\""
            };
            formData.Add(fileContent);

            // Add version information to the form data
            formData.Add(new StringContent(version), "versionNumber");
            formData.Add(new StringContent(description), "versionDescription");

            // Log the info for debugging
            string logInfo = $"name {fileName} --versionNumber {version} --versionDescription {description}";
            _logger.LogDebug("HTTP Post with info: " + logInfo);

            // Make the HTTP POST request
            var response = await _httpClient.PostAsync(versionUploadPath, formData, cancellationToken).ConfigureAwait(false);

            // Read and log the response body
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug($"Response: {response.StatusCode}, Body: {body}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"The version upload api returned a {response.StatusCode} response. Body: {body}");

                // Problem, Deployer cannot handle the new flow of catalog upload. So we will use the 'old' upload again. Return that Identifier.

                VolatileContentType volatileType;
                if (fileName.EndsWith(".dmprotocol",StringComparison.InvariantCultureIgnoreCase))
                {
                    volatileType = VolatileContentType.Connector;
                }
                else
                {
                    volatileType = VolatileContentType.DmScript;
                }

                try
                {
                    CatalogMetaData meta = new CatalogMetaData() { Name = fileName, Version = new CatalogVersionMetaData() { Value = version } };
                    return await VolatileArtifactUploadAsync(package, volatileType, key, meta, cancellationToken);
                }
                catch
                {
                    // Do Nothing, this is a workaround anyway.
                    return new ArtifactUploadResult() { ArtifactId = "" };
                }

                //var returnedResult = JsonConvert.DeserializeObject<CatalogUploadResult>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                //return new ArtifactUploadResult() { ArtifactId = returnedResult.AzureStorageId };
            }

            _logger.LogError($"The version upload api returned a {response.StatusCode} response. Body: {body}");
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                throw new AuthenticationException($"The version upload api returned a {response.StatusCode} response. Body: {body}");
            }

            throw new InvalidOperationException($"The version upload api returned a {response.StatusCode} response. Body: {body}");
        }

        public async Task<ArtifactUploadResult> VolatileArtifactUploadAsync(byte[] package, VolatileContentType type, string key, CatalogMetaData catalog, CancellationToken cancellationToken)
        {
            using var formData = new MultipartFormDataContent();
            formData.Headers.Add("Ocp-Apim-Subscription-Key", key);
            formData.Add(new StringContent(catalog.Name), "name");
            formData.Add(new StringContent(catalog.Version.Value), "version");

            string oldApiContentType = type.ToString();

            formData.Add(new StringContent(oldApiContentType), "contentType");

            using MemoryStream ms = new MemoryStream();
            ms.Write(package, 0, package.Length);

            // Reset position so it can be read out again.
            ms.Position = 0;
            formData.Add(new StreamContent(ms), "file", catalog.Name);

            string logInfo = $"--name {catalog.Name} --version {catalog.Version.Value} --contentType {catalog.ContentType}  --file {catalog.Name}";

            _logger.LogDebug("HTTP Post with info:" + logInfo);

            var response = await _httpClient.PostAsync(VolatileUploadPath, formData, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"The upload api returned a {response.StatusCode} response. Body: {body}");
                return JsonConvert.DeserializeObject<ArtifactUploadResult>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            }

            _logger.LogError($"The upload api returned a {response.StatusCode} response. Body: {body}");
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                throw new AuthenticationException($"The upload api returned a {response.StatusCode} response. Body: {body}");
            }

            throw new InvalidOperationException($"The upload api returned a {response.StatusCode} response. Body: {body}");
        }

        public async Task<ArtifactUploadResult> VolatileArtifactUploadAsync(byte[] package, string key, CatalogMetaData catalog, CancellationToken cancellationToken)
        {
           return await VolatileArtifactUploadAsync(package,VolatileContentType.DmScript,key, catalog, cancellationToken);
        }
    }
}