namespace Skyline.DataMiner.CICD.Tools.CatalogUpload
{
    using System.CommandLine;
    using System.Threading.Tasks;

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
            var exampleArgument = new Option<string>(
                name: "--exampleArgument",
                description: "Just an example argument.")
            {
                IsRequired = true
            };

            var rootCommand = new RootCommand("Uploads artifacts to the Skyline DataMiner catalog (https://catalog.dataminer.services)")
            {
                exampleArgument,
            };

            rootCommand.SetHandler(Process, exampleArgument);

            await rootCommand.InvokeAsync(args);

            return 0;
        }

        private static async Task Process(string exampleArgument)
        {
            //Main Code for program here
        }
    }
}