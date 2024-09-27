﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using CICD.Tools.CatalogUpload;
using Skyline.DataMiner.CICD.FileSystem;
using Moq;
using Microsoft.Extensions.Logging;
using Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib;
using FluentAssertions;
using System.Threading.Tasks;

namespace CICD.Tools.CatalogUpload.Tests
{
	[TestClass()]
	public class UploaderTests
	{
		[TestMethod]
		public async Task ProcessWithRegistrationAsync_ShouldApplyArtifact_Yaml_AndArgumentsInCorrectOrder()
		{
			// Arrange
			var mockFileSystem = new Mock<IFileSystem>();
			var mockCatalogMetaDataFactory = new Mock<ICatalogMetaDataFactory>();

			// Mock the initial artifact metadata
			var artifactMetaData = new CatalogMetaData
			{
				CatalogIdentifier = "artifact-id",
				Name = "ArtifactName",
				Version = new CatalogVersionMetaData { Value = "artifact-version" }
			};

			// Mock the factory to return the artifact metadata
			mockCatalogMetaDataFactory.Setup(factory => factory.FromArtifact(It.IsAny<string>(), null, null))
				.Returns(artifactMetaData);

			// Mock YAML data to overwrite some fields
			mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>()))
				.Returns(@"
                    id: yaml-id
                    source_code_url: yaml-source-uri
                ");

			mockFileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
			mockFileSystem.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>())).Returns(new string[] { "catalog.yml" });
			mockFileSystem.Setup(fs => fs.Directory.GetFiles(It.IsAny<string>())).Returns(new string[] { "catalog.yml" });
			mockFileSystem.Setup(fs => fs.Path.GetFileName("catalog.yml")).Returns("catalog.yml");

			// Act
			var uploader = new Uploader(mockFileSystem.Object, true, mockCatalogMetaDataFactory.Object);

			await uploader.ProcessWithRegistrationAsync(
				dmCatalogToken: "dummyToken",
				pathToArtifact: "test/pathToArtifact.dmapp",
				uriSourceCode: null,
				overrideVersion: "argument-version", // This should override the artifact and YAML version
				branch: "argument-branch", // This should be applied
				committerMail: "argument-committer", // This should be applied
				releaseUri: "argument-release-uri" // This should be applied
			);

			// Assert
			// Check the order of data application:
			// 1. Metadata should start from the artifact.
			artifactMetaData.Name.Should().Be("ArtifactName"); // Never overwritten

			// 2. YAML should overwrite the artifact.
			artifactMetaData.CatalogIdentifier.Should().Be("yaml-id"); // Applied from YAML
			artifactMetaData.SourceCodeUri.Should().Be("yaml-source-uri"); // Overwritten by YAML

			// 3. Arguments should overwrite both the artifact and the YAML.
			artifactMetaData.Version.Value.Should().Be("argument-version"); // Overwritten by input argument
			artifactMetaData.Version.Branch.Should().Be("argument-branch"); // Applied from input argument
			artifactMetaData.Version.CommitterMail.Should().Be("argument-committer"); // Applied from input argument
			artifactMetaData.Version.ReleaseUri.Should().Be("argument-release-uri"); // Applied from input argument
		}
	}
}