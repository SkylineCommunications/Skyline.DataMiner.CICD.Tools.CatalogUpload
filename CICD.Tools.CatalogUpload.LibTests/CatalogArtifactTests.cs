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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private Mock<ILogger> fakeLogger;
		private ILogger logger;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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

			var originalKey_encrypt = Environment.GetEnvironmentVariable("dmcatalogtoken_encrypted", EnvironmentVariableTarget.Machine) ?? "";
			var originalKey = Environment.GetEnvironmentVariable("dmcatalogtoken") ?? "";

			try
			{
				Environment.SetEnvironmentVariable("dmcatalogtoken_encrypted", "", EnvironmentVariableTarget.Machine);
				Environment.SetEnvironmentVariable("dmcatalogtoken", "");

				CatalogArtifact artifactModel = new CatalogArtifact(pathToArtifact, fakeService.Object, fakeFileSystem.Object, logger, metaData);
				Func<Task> uploadAction = async () => { await artifactModel.UploadAsync(); };
				await uploadAction.Should().ThrowAsync<InvalidOperationException>().WithMessage("*missing token*");
			}
			finally
			{
				Environment.SetEnvironmentVariable("dmcatalogtoken_encrypted", originalKey_encrypt, EnvironmentVariableTarget.Machine);
				Environment.SetEnvironmentVariable("dmcatalogtoken", originalKey);
			}
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

			var originalKey_encrypt = Environment.GetEnvironmentVariable("dmcatalogtoken_encrypted", EnvironmentVariableTarget.Machine) ?? "";
			var originalKey = Environment.GetEnvironmentVariable("dmcatalogtoken") ?? "";

			try
			{
				Environment.SetEnvironmentVariable("dmcatalogtoken", "");

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
				Environment.SetEnvironmentVariable("dmcatalogtoken_encrypted", originalKey_encrypt, EnvironmentVariableTarget.Machine);
				Environment.SetEnvironmentVariable("dmcatalogtoken", originalKey);
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

			var originalKey_encrypt = Environment.GetEnvironmentVariable("dmcatalogtoken_encrypted", EnvironmentVariableTarget.Machine) ?? "";
			var originalKey = Environment.GetEnvironmentVariable("dmcatalogtoken") ?? "";

			try
			{
				Environment.SetEnvironmentVariable("dmcatalogtoken_encrypted", "", EnvironmentVariableTarget.Machine);
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
				Environment.SetEnvironmentVariable("dmcatalogtoken_encrypted", originalKey_encrypt, EnvironmentVariableTarget.Machine);
				Environment.SetEnvironmentVariable("dmcatalogtoken", originalKey);
			}
		}
	}
}