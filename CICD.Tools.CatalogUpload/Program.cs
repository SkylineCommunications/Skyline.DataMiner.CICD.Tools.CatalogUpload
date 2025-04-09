namespace Skyline.DataMiner.CICD.Tools.CatalogUpload
{
    using System.CommandLine;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Serilog;

    using Skyline.DataMiner.CICD.FileSystem;
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
            description: "The key to upload to the catalog as defined in admin.dataminer.services. Important! For Volatile Uploads this should be the DataMiner System Token. For Registration Uploads this should be the Organization Token. This is optional if the key can also be provided using the 'DATAMINER_CATALOG_TOKEN' environment variable (unix/win) or using 'DATAMINER_CATALOG_TOKEN_ENCRYPTED' configured with Skyline.DataMiner.CICD.Tools.WinEncryptedKeys (windows).")
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
            description: "A Uri for the globally unique location of your sourcecode (not your local workspace). This can be used as a backup to find your artifact. e.g. https://github.com/SkylineCommunications/MyTestRepo")
            {
                IsRequired = false,
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

            var catalogIdentifier = new Option<string>(
            name: "--catalog-identifier",
            description: "The GUID identifying the catalog item on https://catalog.dataminer.services/. When not provided, this should be present through a catalog.yml file as described here https://docs.dataminer.services/user-guide/Cloud_Platform/Catalog/Register_Catalog_Item.html#manifest-file.")
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
                IsRequired = false,
            };

            var images = new Option<string>(
            name: "--path-to-images",
            description: "Path to a folder with images used in the readme file.")
            {
                IsRequired = false,
            };

            var optionalCatalogDetailsYml = new Option<string>(
                name: "--path-to-catalog-yml",
                description: "Path to a yml file containing catalog details as described here https://docs.dataminer.services/user-guide/Cloud_Platform/Catalog/Register_Catalog_Item.html#manifest-file")
            {
                IsRequired = false,
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
                releaseUri,
                catalogIdentifier,
                optionalCatalogDetailsYml,
                readme,
                images
            };

            var withOnlyRegistrationCommand = new Command("update-catalog-details", "Uploads only the registration information for a Skyline DataMiner catalog (https://catalog.dataminer.services) item.")
            {
                catalogDetailsYml,
                readme,
                images
            };

            rootCommand.SetHandler(ProcessVolatile, pathToArtifactRequired, dmCatalogToken, isDebug);
            withRegistrationCommand.SetHandler(ProcessWithRegistrationAsync, dmCatalogToken, isDebug, pathToArtifactOptional, new OptionalRegistrationArgumentsBinder(uriSourceCode, overrideVersion, branch, committerMail, releaseUri, catalogIdentifier, optionalCatalogDetailsYml, readme, images));
            withOnlyRegistrationCommand.SetHandler(ProcessYmlRegistrationAsync, dmCatalogToken, isDebug, catalogDetailsYml, readme, images);

            rootCommand.Add(withOnlyRegistrationCommand);
            rootCommand.Add(withRegistrationCommand);
            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private static Microsoft.Extensions.Logging.ILogger CreateLogger(bool isDebug)
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

            return loggerFactory.CreateLogger("Skyline.DataMiner.CICD.Tools.CatalogUpload");
        }

        private static async Task<int> ProcessVolatile(string pathToArtifact, string dmCatalogToken, bool isDebug)
        {
            IFileSystem fs = FileSystem.Instance;
            var catalogMetaDataFactory = new CatalogMetaDataFactory();
            var logger = CreateLogger(isDebug);
            var catalogService = CatalogServiceFactory.CreateWithHttp(new System.Net.Http.HttpClient(), logger);
            Uploader uploader = new Uploader(fs, logger, catalogService, catalogMetaDataFactory);
            return await uploader.ProcessVolatile(pathToArtifact, dmCatalogToken).ConfigureAwait(false);
        }

        private static async Task<int> ProcessWithRegistrationAsync(string dmCatalogToken, bool isDebug, string pathToArtifact, OptionalRegistrationArguments optionalArguments)
        {
            IFileSystem fs = FileSystem.Instance;
            var catalogMetaDataFactory = new CatalogMetaDataFactory();
            var logger = CreateLogger(isDebug);
            var catalogService = CatalogServiceFactory.CreateWithHttp(new System.Net.Http.HttpClient(), logger);
            Uploader uploader = new Uploader(fs, logger, catalogService, catalogMetaDataFactory);
            return await uploader.ProcessWithRegistrationAsync(dmCatalogToken, pathToArtifact, optionalArguments).ConfigureAwait(false);
        }

        private static async Task<int> ProcessYmlRegistrationAsync(string dmCatalogToken, bool isDebug, string catalogDetailsYml, string readme, string images)
        {
            IFileSystem fs = FileSystem.Instance;
            var catalogMetaDataFactory = new CatalogMetaDataFactory();
            var logger = CreateLogger(isDebug);
            var catalogService = CatalogServiceFactory.CreateWithHttp(new System.Net.Http.HttpClient(), logger);
            Uploader uploader = new Uploader(fs, logger, catalogService, catalogMetaDataFactory);
            return await uploader.ProcessYmlRegistrationAsync(dmCatalogToken, catalogDetailsYml, readme, images).ConfigureAwait(false);
        }
    }
}