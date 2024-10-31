namespace CICD.Tools.CatalogUpload.LibTests
{
    using System;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Mock;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;

    [TestClass]
    public class CatalogArtifactTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private Mock<ILogger> fakeLogger;
        private ILogger logger;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Initialize()
        {
            fakeLogger = new Mock<ILogger>();
            IServiceCollection services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConsole();
                builder.AddMock(fakeLogger);
            });

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger("TestLogger");
        }

        [TestMethod]
        public async Task UploadAsyncTest_NoToken()
        {
            string pathToArtifact = "";
            Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
            Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();
            CatalogVersionMetaData versionMeta = new CatalogVersionMetaData
            {
                Branch = "1.0.0.X",
                CommitterMail = "thunder@skyline.be",
                ReleaseUri = "pathToNotes",
                Value = "1.0.0.1-alpha"
            };

            CatalogMetaData metaData = new CatalogMetaData
            {

                ContentType = "DMScript",
                SourceCodeUri = "uniqueIdentifier",
                Name = "Name",
                Version = versionMeta
            };

            var originalKeyEncrypt = Environment.GetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", EnvironmentVariableTarget.Machine) ?? "";
            var originalKey = Environment.GetEnvironmentVariable("DATAMINER_CATALOG_TOKEN") ?? "";

            try
            {
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", "", EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN", "");

                CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
                Func<Task> uploadAction = async () => await artifactModel.VolatileUploadAsync();

                await uploadAction.Should().ThrowAsync<InvalidOperationException>().WithMessage("*missing token*");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", originalKeyEncrypt, EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN", originalKey);
            }
        }

        [TestMethod]
        public async Task UploadAsyncTest_ProvidedEncryptedTokenEnvironment()
        {
            // Arrange
            string pathToArtifact = "";
            Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
            Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();

            CatalogVersionMetaData versionMeta = new CatalogVersionMetaData
            {
                Branch = "1.0.0.X",
                CommitterMail = "thunder@skyline.be",
                ReleaseUri = "pathToNotes",
                Value = "1.0.0.1-alpha"
            };

            CatalogMetaData metaData = new CatalogMetaData
            {
                ContentType = "DMScript",
                SourceCodeUri = "uniqueIdentifier",
                Name = "Name",
                Version = versionMeta
            };

            Mock<IFileIO> fakeFile = new Mock<IFileIO>();
            fakeFile.Setup(p => p.ReadAllBytes(It.IsAny<string>())).Returns(Array.Empty<byte>());
            fakeFileSystem.Setup(p => p.File).Returns(fakeFile.Object);

            ArtifactUploadResult model = new ArtifactUploadResult
            {
                ArtifactId = "10"
            };

            fakeService.Setup(p => p.VolatileArtifactUploadAsync(It.IsAny<byte[]>(), "encryptedFake", metaData, It.IsAny<CancellationToken>())).ReturnsAsync(model);

            var originalKeyEncrypt = Environment.GetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", EnvironmentVariableTarget.Machine) ?? "";
            var originalKey = Environment.GetEnvironmentVariable("DATAMINER_CATALOG_TOKEN") ?? "";

            try
            {
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN", "");

                Skyline.DataMiner.CICD.Tools.WinEncryptedKeys.Lib.Keys.SetKey("DATAMINER_CATALOG_TOKEN_ENCRYPTED", "encryptedFake");

                // Act
                CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
                var result = await artifactModel.VolatileUploadAsync();

                // Assert
                result.ArtifactId.Should().Be("10");

                fakeLogger.VerifyLog().InformationWasCalled().MessageEquals(@"{""artifactId"":""10""}");

                fakeService.VerifyAll();
                fakeFileSystem.VerifyAll();
            }
            finally
            {
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", originalKeyEncrypt, EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN", originalKey);
            }
        }

        [TestMethod]
        public async Task UploadAsyncTest_ProvidedTokenArgument()
        {
            // Arrange
            string pathToArtifact = "";
            Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
            Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();

            CatalogVersionMetaData versionMeta = new CatalogVersionMetaData
            {
                Branch = "1.0.0.X",
                CommitterMail = "thunder@skyline.be",
                ReleaseUri = "pathToNotes",
                Value = "1.0.0.1-alpha"
            };

            CatalogMetaData metaData = new CatalogMetaData
            {
                ContentType = "DMScript",
                SourceCodeUri = "uniqueIdentifier",
                Name = "Name",
                Version = versionMeta
            };

            Mock<IFileIO> fakeFile = new Mock<IFileIO>();
            fakeFile.Setup(p => p.ReadAllBytes(It.IsAny<string>())).Returns(Array.Empty<byte>());
            fakeFileSystem.Setup(p => p.File).Returns(fakeFile.Object);

            ArtifactUploadResult model = new ArtifactUploadResult
            {
                ArtifactId = "10"
            };

            fakeService.Setup(p => p.VolatileArtifactUploadAsync(It.IsAny<byte[]>(), "token", metaData, It.IsAny<CancellationToken>())).ReturnsAsync(model);

            // Act
            CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
            var result = await artifactModel.VolatileUploadAsync("token");

            // Assert
            result.ArtifactId.Should().Be("10");

            fakeLogger.VerifyLog().InformationWasCalled().MessageEquals(@"{""artifactId"":""10""}");

            fakeService.VerifyAll();
            fakeFileSystem.VerifyAll();
        }

        [TestMethod]
        public async Task UploadAsyncTest_ProvidedTokenEnvironment()
        {
            // Arrange
            string pathToArtifact = "";
            Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
            Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();

            CatalogVersionMetaData versionMeta = new CatalogVersionMetaData
            {
                Branch = "1.0.0.X",
                CommitterMail = "thunder@skyline.be",
                ReleaseUri = "pathToNotes",
                Value = "1.0.0.1-alpha"
            };

            CatalogMetaData metaData = new CatalogMetaData
            {
                ContentType = "DMScript",
                SourceCodeUri = "uniqueIdentifier",
                Name = "Name",
                Version = versionMeta
            };

            Mock<IFileIO> fakeFile = new Mock<IFileIO>();
            fakeFile.Setup(p => p.ReadAllBytes(It.IsAny<string>())).Returns(Array.Empty<byte>());
            fakeFileSystem.Setup(p => p.File).Returns(fakeFile.Object);

            ArtifactUploadResult model = new ArtifactUploadResult
            {
                ArtifactId = "10"
            };

            fakeService.Setup(p => p.VolatileArtifactUploadAsync(It.IsAny<byte[]>(), "fake", metaData, It.IsAny<CancellationToken>())).ReturnsAsync(model);

            var originalKeyEncrypt = Environment.GetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", EnvironmentVariableTarget.Machine) ?? "";
            var originalKey = Environment.GetEnvironmentVariable("DATAMINER_CATALOG_TOKEN") ?? "";

            try
            {
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", "", EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN", "fake");

                // Act
                CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
                var result = await artifactModel.VolatileUploadAsync();

                // Assert
                result.ArtifactId.Should().Be("10");

                fakeLogger.VerifyLog().InformationWasCalled().MessageEquals(@"{""artifactId"":""10""}");

                fakeService.VerifyAll();
                fakeFileSystem.VerifyAll();
            }
            finally
            {
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN_ENCRYPTED", originalKeyEncrypt, EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("DATAMINER_CATALOG_TOKEN", originalKey);
            }
        }

        [TestMethod]
        public async Task VolatileUploadAsync_ShouldLogInformation_WhenCalled()
        {
            // Arrange
            string pathToArtifact = "testPath";
            var fakeService = new Mock<ICatalogService>();
            var fakeFileSystem = new Mock<IFileSystem>();
            var uploadResult = new ArtifactUploadResult { ArtifactId = "10" };
            fakeFileSystem.Setup(fs => fs.File.ReadAllBytes(It.IsAny<string>())).Returns(Array.Empty<byte>());
            fakeService
                .Setup(service => service.VolatileArtifactUploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CatalogMetaData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadResult);

            fakeFileSystem.Setup(fs => fs.Path.GetFileName(pathToArtifact)).Returns(pathToArtifact);

            CatalogMetaData metaData = new CatalogMetaData
            {
                CatalogIdentifier = "123",
                ContentType = "DMScript",
                SourceCodeUri = "uniqueIdentifier",
                Name = "Name",
                Version = new CatalogVersionMetaData { Value = "1.0.0.1-alpha" }
            };
            CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);

            // Act
            var result = await artifactModel.VolatileUploadAsync("dummyToken");

            // Assert
            result.Should().Be(uploadResult);
            fakeLogger.Verify(
              logger => logger.Log(
                  LogLevel.Information,
                  It.IsAny<EventId>(),
                  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(JsonConvert.SerializeObject(uploadResult))),
                  It.IsAny<Exception>(),
                  (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
              Times.Once);
        }

        [TestMethod]
        public async Task UploadAndRegisterAsync_ShouldLogInformation_WhenCalled()
        {
            // Arrange
            string pathToArtifact = "testPath";
            var fakeService = new Mock<ICatalogService>();
            var fakeFileSystem = new Mock<IFileSystem>();
            var uploadResult = new ArtifactUploadResult { ArtifactId = "10" };
            fakeFileSystem.Setup(fs => fs.File.ReadAllBytes(It.IsAny<string>())).Returns(Array.Empty<byte>());
            fakeService
                .Setup(service => service.RegisterCatalogAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadResult);
            fakeService
                .Setup(service => service.UploadVersionAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadResult);

            fakeFileSystem.Setup(fs => fs.Path.GetFileName(pathToArtifact)).Returns(pathToArtifact);

            CatalogMetaData metaData = new CatalogMetaData
            {
                CatalogIdentifier = "123",
                ContentType = "DMScript",
                SourceCodeUri = "uniqueIdentifier",
                Name = "Name",
                Version = new CatalogVersionMetaData { Value = "1.0.0.1-alpha" }
            };
            CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);

            // Act
            var result = await artifactModel.UploadAndRegisterAsync("dummyToken");

            // Assert
            result.Should().Be(uploadResult);
            fakeLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(JsonConvert.SerializeObject(uploadResult))),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);

        }

    }
}