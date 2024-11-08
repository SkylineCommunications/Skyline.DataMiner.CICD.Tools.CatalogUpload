namespace Skyline.DataMiner.CICD.Tools.CatalogUpload
{
    using System.CommandLine;
    using System.CommandLine.Binding;

    /// <summary>
    /// Represents optional arguments that can be used during the artifact registration process.
    /// </summary>
    public class OptionalRegistrationArguments
    {
        /// <summary>
        /// Gets or sets the branch of the source code repository where the artifact was built from.
        /// </summary>
        public string Branch { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the catalog item associated with the artifact.
        /// </summary>
        public string CatalogIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the email of the committer responsible for the changes in the source code.
        /// </summary>
        public string CommitterMail { get; set; }

        /// <summary>
        /// Gets or sets the version to override the default version for the artifact registration.
        /// </summary>
        public string OverrideVersion { get; set; }

        /// <summary>
        /// Gets or sets the URI that links to the release information for the artifact.
        /// </summary>
        public string ReleaseUri { get; set; }

        /// <summary>
        /// Gets or sets the URI to the source code related to the artifact.
        /// </summary>
        public string UriSourceCode { get; set; }

        /// <summary>
        /// Gets or sets the path for the catalog.yml or manifest.yml file.
        /// </summary>
        public string PathToCatalogYml { get; set; }

        /// <summary>
        /// Gets or sets the path for the readme file.
        /// </summary>
        public string PathToReadme { get; set; }

        /// <summary>
        /// Gets or sets the path for the images directory.
        /// </summary>
        public string PathToImages { get; set; }
    }

    internal class OptionalRegistrationArgumentsBinder : BinderBase<OptionalRegistrationArguments>
    {
        private readonly Option<string> branch;
        private readonly Option<string> catalogIdentifier;
        private readonly Option<string> committerMail;
        private readonly Option<string> overrideVersion;
        private readonly Option<string> releaseUri;
        private readonly Option<string> uriSourceCode;
        private readonly Option<string> pathToCatalogYml;
        private readonly Option<string> pathToReadme;
        private readonly Option<string> pathToImages;

        /// <summary>
        /// Binds command line options to <see cref="OptionalRegistrationArguments"/>.
        /// </summary>
        public OptionalRegistrationArgumentsBinder(Option<string> uriSourceCode, Option<string> overrideVersion, Option<string> branch, Option<string> committerMail, Option<string> releaseUri, Option<string> catalogIdentifier, Option<string> pathToCatalogYml, Option<string> pathToReadme, Option<string> pathToImages)
        {
            this.uriSourceCode = uriSourceCode;
            this.overrideVersion = overrideVersion;
            this.branch = branch;
            this.committerMail = committerMail;
            this.releaseUri = releaseUri;
            this.catalogIdentifier = catalogIdentifier;
            this.pathToCatalogYml = pathToCatalogYml;
            this.pathToReadme = pathToReadme;
            this.pathToImages = pathToImages;
        }

        /// <summary>
        /// Retrieves the bound value of <see cref="OptionalRegistrationArguments"/> from the <see cref="BindingContext"/>.
        /// </summary>
        /// <param name="bindingContext">The context containing parsed command line arguments.</param>
        /// <returns>An instance of <see cref="OptionalRegistrationArguments"/> populated with values obtained from the command line options.</returns>
        /// <remarks>
        /// This method overrides the base <see cref="BinderBase{T}.GetBoundValue"/> method to provide specific logic for binding command line options to the properties of <see cref="OptionalRegistrationArguments"/>.
        /// It extracts values for each option defined in the command line arguments and assigns them to the corresponding properties of a new <see cref="OptionalRegistrationArguments"/> instance.
        /// </remarks>
        protected override OptionalRegistrationArguments GetBoundValue(BindingContext bindingContext)
        {
            return new OptionalRegistrationArguments
            {
                UriSourceCode = bindingContext.ParseResult.GetValueForOption(uriSourceCode),
                OverrideVersion = bindingContext.ParseResult.GetValueForOption(overrideVersion),
                Branch = bindingContext.ParseResult.GetValueForOption(branch),
                CommitterMail = bindingContext.ParseResult.GetValueForOption(committerMail),
                ReleaseUri = bindingContext.ParseResult.GetValueForOption(releaseUri),
                CatalogIdentifier = bindingContext.ParseResult.GetValueForOption(catalogIdentifier),
                PathToCatalogYml = bindingContext.ParseResult.GetValueForOption(pathToCatalogYml),
                PathToReadme = bindingContext.ParseResult.GetValueForOption(pathToReadme),
                PathToImages = bindingContext.ParseResult.GetValueForOption(pathToImages)
            };
        }
    }
}