using AdPlacementService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;

namespace AdPlacementService.Tests
{
    public class AdPlacementsControllerTests
    {
        private readonly Mock<ILogger<AdPlacementsController>> _mockLogger;
        private readonly AdPlacementsController _controller;

        public AdPlacementsControllerTests()
        {
            _mockLogger = new Mock<ILogger<AdPlacementsController>>();
            _controller = new AdPlacementsController(_mockLogger.Object);
        }

        [Fact]
        public void LoadAdPlacementsFromFile_ValidFileContent_ReturnsOkResult()
        {
            // Arrange
            string fileContent = "Яндекс.Директ:/ru\nКрутая реклама:/ru/svrd";

            // Act
            var result = _controller.LoadAdPlacementsFromFile(fileContent) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Value.Should().Be("Successfully loaded 2 ad placements from file.");
        }

        [Fact]
        public void LoadAdPlacementsFromFile_EmptyFileContent_ReturnsBadRequestResult()
        {
            // Arrange
            string fileContent = "";

            // Act
            var result = _controller.LoadAdPlacementsFromFile(fileContent) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
            result.Value.Should().Be("File content cannot be null or empty.");
        }

        [Fact]
        public void SearchAdPlacements_ValidLocation_ReturnsOkResultWithAdPlacements()
        {
            // Arrange
            string fileContent = "Яндекс.Директ:/ru\nКрутая реклама:/ru/svrd";
            _controller.LoadAdPlacementsFromFile(fileContent); // Load some data

            string location = "/ru/svrd";

            // Act
            var result = _controller.SearchAdPlacements(location) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);

            var placements = result.Value as List<AdPlacementsController.AdPlacement>;
            placements.Should().NotBeNull();
            placements.Count.Should().Be(2); //Check counts match expected
            placements.Any(p => p.Name == "Крутая реклама").Should().BeTrue(); //Check specific values exist

        }

        [Fact]
        public void SearchAdPlacements_InvalidLocation_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            string location = "/nonexistent";
            string fileContent = "Яндекс.Директ:/ru\nКрутая реклама:/ru/svrd";
            _controller.LoadAdPlacementsFromFile(fileContent);

            // Act
            var result = _controller.SearchAdPlacements(location) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);

            var placements = result.Value as List<AdPlacementsController.AdPlacement>;
            placements.Should().NotBeNull();
            placements.Count.Should().Be(0); //Expect an empty result list
        }

        [Fact]
        public void SearchAdPlacements_NullLocation_ReturnsBadRequest()
        {
            // Arrange
            string location = null;

            // Act
            var result = _controller.SearchAdPlacements(location) as BadRequestObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
            result.Value.Should().Be("Location parameter is required.");
        }
    }
}