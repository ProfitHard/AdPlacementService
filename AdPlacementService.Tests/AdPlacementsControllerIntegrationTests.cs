using AdPlacementService;
using AdPlacementService.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using Newtonsoft.Json; // Install Newtonsoft.Json NuGet Package
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace AdPlacementService.Tests
{
    public class AdPlacementsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public AdPlacementsControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task LoadAdPlacementsFromFile_ValidFileContent_ReturnsOkResult()
        {
            // Arrange
            string fileContent = "Яндекс.Директ:/ru\nКрутая реклама:/ru/svrd";
            StringContent content = new StringContent(fileContent, Encoding.UTF8, "application/json"); //Send as JSON (even though it's a raw string)

            // Act
            var response = await _client.PostAsync("api/v1.0/AdPlacements/LoadFromFile", content);  //Use the correct URL (including version)

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            string responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Successfully loaded 2 ad placements from file.");
        }

        [Fact]
        public async Task SearchAdPlacements_ValidLocation_ReturnsOkResultWithAdPlacements()
        {
            // Arrange
            string fileContent = "Яндекс.Директ:/ru\nКрутая реклама:/ru/svrd";
            StringContent loadContent = new StringContent(fileContent, Encoding.UTF8, "application/json"); //JSON

            await _client.PostAsync("api/v1.0/AdPlacements/LoadFromFile", loadContent);  //LOAD FIRST

            string location = "/ru/svrd";

            // Act
            var response = await _client.GetAsync($"api/v1.0/AdPlacements/Search?location={location}"); //URL Encode

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Deserialize the JSON response into a list of AdPlacement objects
            var placements = JsonConvert.DeserializeObject<List<AdPlacementsController.AdPlacement>>(responseContent);

            placements.Should().NotBeNull();
            placements.Count.Should().Be(2);
            placements.Any(p => p.Name == "Крутая реклама").Should().BeTrue();
        }

        [Fact]
        public async Task SearchAdPlacements_InvalidLocation_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            string fileContent = "Яндекс.Директ:/ru\nКрутая реклама:/ru/svrd";
            StringContent loadContent = new StringContent(fileContent, Encoding.UTF8, "application/json"); //JSON

            await _client.PostAsync("api/v1.0/AdPlacements/LoadFromFile", loadContent);

            string location = "/nonexistent";

            // Act
            var response = await _client.GetAsync($"api/v1.0/AdPlacements/Search?location={location}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Deserialize the JSON response into a list of AdPlacement objects
            var placements = JsonConvert.DeserializeObject<List<AdPlacementsController.AdPlacement>>(responseContent);

            placements.Should().NotBeNull();
            placements.Count.Should().Be(0);
        }

        [Fact]
        public async Task SearchAdPlacements_NullLocation_ReturnsBadRequest()
        {
            // Arrange

            string location = null;

            // Act
            var response = await _client.GetAsync($"api/v1.0/AdPlacements/Search?location={location}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        }
    }
}