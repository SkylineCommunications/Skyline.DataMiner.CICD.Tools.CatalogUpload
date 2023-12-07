using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Skyline.DataMiner.CICD.Tools.CatalogUpload.Lib.Tests
{
	[TestClass()]
	public class CatalogMetaDataTests
	{
		[TestMethod()]
		public void FromArtifactTest_BuildPreReleaseDmappWithProtocols()
		{
			// Arrange
			string pathToArtifact = "TestData/SLNetSubscriptionsBenchmarking 1.0.1_B15.dmapp";

			// Act
			CatalogMetaData result = CatalogMetaData.FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "Package",
				Name = "SLNetSubscriptionsBenchmarking",
				Version = "1.0.1-B15",
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeTrue();
		}

		[TestMethod()]
		public void FromArtifactTest_ReleaseAutomation()
		{
			// Arrange
			string pathToArtifact = "TestData/Demo InterAppCalls 1.0.0-CU1.dmapp";

			// Act
			CatalogMetaData result = CatalogMetaData.FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "DmScript",
				Name = "Demo InterAppCalls",
				Version = "1.0.0-CU1",
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeFalse();
		}

		[TestMethod()]
		public void FromArtifactTest_ReleaseDashboard()
		{
			// Arrange
			string pathToArtifact = "TestData/Tandberg RX1290 1.0.0-CU1.dmapp";

			// Act
			CatalogMetaData result = CatalogMetaData.FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "Dashboard",
				Name = "Tandberg RX1290",
				Version = "1.0.0-CU1",
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeFalse();
		}

		[TestMethod()]
		public void FromArtifactTest_ReleaseProtocolVisio()
		{
			// Arrange
			string pathToArtifact = "TestData/Microsoft Platform 1.0.0-CU4.dmapp";

			// Act
			CatalogMetaData result = CatalogMetaData.FromArtifact(pathToArtifact);

			// Assert

			CatalogMetaData expected = new CatalogMetaData()
			{
				ContentType = "Visio",
				Name = "Microsoft Platform",
				Version = "1.0.0-CU4",
			};

			result.Should().Be(expected);
			result.IsPreRelease().Should().BeFalse();
		}
	}
}