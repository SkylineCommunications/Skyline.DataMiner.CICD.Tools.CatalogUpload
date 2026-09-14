namespace CICD.Tools.CatalogUpload.LibTests.CatalogService
{
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public class HttpCatalogServiceTests
    {
        [TestMethod]
        public async Task IsCatalogItemPrivate_IsPublic()
        {
            // Arrange
            const string catalogGuid = "4abcf220-c001-4ffd-bab8-559dee47088f"; // Microsoft Platform

            // Act
            ICatalogService catalogService = CatalogServiceFactory.CreateWithHttp(new HttpClient(), NullLogger.Instance);
            bool isPrivate = await catalogService.IsCatalogItemPrivate(catalogGuid, Environment.GetEnvironmentVariable("skyline__sdk__dataminertoken", EnvironmentVariableTarget.Machine), CancellationToken.None);

            // Assert
            Assert.IsFalse(isPrivate);
        }

        [TestMethod]
        public async Task IsCatalogItemPrivate_IsPrivate()
        {
            // Arrange
            const string catalogGuid = "671f7a9f-cfff-4f69-80f2-a9aca160444b"; // BOOST-DailyRegression-SharedLibrary

            // Act
            ICatalogService catalogService = CatalogServiceFactory.CreateWithHttp(new HttpClient(), NullLogger.Instance);
            bool isPrivate = await catalogService.IsCatalogItemPrivate(catalogGuid, Environment.GetEnvironmentVariable("skyline__sdk__dataminertoken", EnvironmentVariableTarget.Machine), CancellationToken.None);

            // Assert
            Assert.IsTrue(isPrivate);
        }
    }
}