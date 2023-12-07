namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.Tests
{
	using System;
	using System.Threading.Tasks;

	using FluentAssertions;

	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Logging.Mock;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Moq;

	using Skyline.DataMiner.CICD.FileSystem;
	using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;

	[TestClass()]
	public class CatalogArtifactTests
	{
		private Mock<ILogger> fakeLogger;
		private ILogger logger;

		[TestInitialize()]
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

		[TestMethod()]
		public async Task UploadAsyncTest_NoToken()
		{
			string pathToArtifact = "";
			Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
			Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();

			CatalogMetaData metaData = new CatalogMetaData()
			{
				Branch = "1.0.0.X",
				CommitterMail = "thunder@skyline.be",
				ContentType = "DMScript",
				Identifier = "uniqueIdentifier",
				Name = "Name",
				ReleaseUri = "pathToNotes",
				Version = "1.0.0.1-alpha"
			};

			CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
			Func<Task> uploadAction = async () => { await artifactModel.UploadAsync(); };
			await uploadAction.Should().ThrowAsync<InvalidOperationException>().WithMessage("*missing token*");
		}

		[TestMethod()]
		public async Task UploadAsyncTest_ProvidedEncryptedTokenEnvironment()
		{
			// Arrange
			string pathToArtifact = "";
			Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
			Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();

			CatalogMetaData metaData = new CatalogMetaData()
			{
				Branch = "1.0.0.X",
				CommitterMail = "thunder@skyline.be",
				ContentType = "DMScript",
				Identifier = "uniqueIdentifier",
				Name = "Name",
				ReleaseUri = "pathToNotes",
				Version = "1.0.0.1-alpha"
			};

			Mock<IFileIO> fakeFile = new Mock<IFileIO>();
			fakeFile.Setup(p => p.ReadAllBytes(It.IsAny<String>())).Returns(new byte[0]);
			fakeFileSystem.Setup(p => p.File).Returns(fakeFile.Object);

			ArtifactUploadResult model = new ArtifactUploadResult();
			model.ArtifactId = "10";

			fakeService.Setup(p => p.ArtifactUploadAsync(It.IsAny<byte[]>(), "encryptedFake", metaData, It.IsAny<CancellationToken>())).ReturnsAsync(model);

			try
			{
				WinEncryptedKeys.Lib.Keys.SetKey("dmcatalogtoken_encrypted", "encryptedFake");

				// Act
				CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
				var result = await artifactModel.UploadAsync();

				// Assert
				result.ArtifactId.Should().Be("10");

				fakeLogger.VerifyLog().InformationWasCalled().MessageEquals(@"{""artifactId"":""10""}");

				fakeService.VerifyAll();
				fakeFileSystem.VerifyAll();
			}
			finally
			{
				Environment.SetEnvironmentVariable("dmcatalogtoken_encrypted", "", EnvironmentVariableTarget.Machine);
			}
		}

		[TestMethod()]
		public async Task UploadAsyncTest_ProvidedTokenArgument()
		{
			// Arrange
			string pathToArtifact = "";
			Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
			Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();

			CatalogMetaData metaData = new CatalogMetaData()
			{
				Branch = "1.0.0.X",
				CommitterMail = "thunder@skyline.be",
				ContentType = "DMScript",
				Identifier = "uniqueIdentifier",
				Name = "Name",
				ReleaseUri = "pathToNotes",
				Version = "1.0.0.1-alpha"
			};

			Mock<IFileIO> fakeFile = new Mock<IFileIO>();
			fakeFile.Setup(p => p.ReadAllBytes(It.IsAny<String>())).Returns(new byte[0]);
			fakeFileSystem.Setup(p => p.File).Returns(fakeFile.Object);

			ArtifactUploadResult model = new ArtifactUploadResult();
			model.ArtifactId = "10";

			fakeService.Setup(p => p.ArtifactUploadAsync(It.IsAny<byte[]>(), "token", metaData, It.IsAny<CancellationToken>())).ReturnsAsync(model);

			// Act
			CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
			var result = await artifactModel.UploadAsync("token");

			// Assert
			result.ArtifactId.Should().Be("10");

			fakeLogger.VerifyLog().InformationWasCalled().MessageEquals(@"{""artifactId"":""10""}");

			fakeService.VerifyAll();
			fakeFileSystem.VerifyAll();
		}

		[TestMethod()]
		public async Task UploadAsyncTest_ProvidedTokenEnvironment()
		{
			// Arrange
			string pathToArtifact = "";
			Mock<ICatalogService> fakeService = new Mock<ICatalogService>();
			Mock<IFileSystem> fakeFileSystem = new Mock<IFileSystem>();

			CatalogMetaData metaData = new CatalogMetaData()
			{
				Branch = "1.0.0.X",
				CommitterMail = "thunder@skyline.be",
				ContentType = "DMScript",
				Identifier = "uniqueIdentifier",
				Name = "Name",
				ReleaseUri = "pathToNotes",
				Version = "1.0.0.1-alpha"
			};

			Mock<IFileIO> fakeFile = new Mock<IFileIO>();
			fakeFile.Setup(p => p.ReadAllBytes(It.IsAny<String>())).Returns(new byte[0]);
			fakeFileSystem.Setup(p => p.File).Returns(fakeFile.Object);

			ArtifactUploadResult model = new ArtifactUploadResult();
			model.ArtifactId = "10";

			fakeService.Setup(p => p.ArtifactUploadAsync(It.IsAny<byte[]>(), "fake", metaData, It.IsAny<CancellationToken>())).ReturnsAsync(model);

			try
			{
				Environment.SetEnvironmentVariable("dmcatalogtoken", "fake");

				// Act
				CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
				var result = await artifactModel.UploadAsync();

				// Assert
				result.ArtifactId.Should().Be("10");

				fakeLogger.VerifyLog().InformationWasCalled().MessageEquals(@"{""artifactId"":""10""}");

				fakeService.VerifyAll();
				fakeFileSystem.VerifyAll();
			}
			finally
			{
				Environment.SetEnvironmentVariable("dmcatalogtoken", String.Empty);
			}
		}
	}
}