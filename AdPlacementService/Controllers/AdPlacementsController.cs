using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.Versioning; // Required for versioning
using Microsoft.Extensions.Logging; // Required for logging

namespace AdPlacementService.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AdPlacementsController : ControllerBase
    {
        private static ConcurrentDictionary<string, AdPlacement> _adPlacementsByName = new ConcurrentDictionary<string, AdPlacement>(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<AdPlacementsController> _logger; // Logger instance

        public AdPlacementsController(ILogger<AdPlacementsController> logger)
        {
            _logger = logger;  // Inject logger through constructor
        }

        public class AdPlacement
        {
            public string Name { get; set; } = "";
            public List<string> Locations { get; set; } = new List<string>();
        }

        [HttpPost("LoadFromFile")]
        public IActionResult LoadAdPlacementsFromFile([FromBody] string fileContent)
        {
            _logger.LogInformation("LoadAdPlacementsFromFile endpoint called."); //Log at entry
            if (string.IsNullOrEmpty(fileContent))
            {
                _logger.LogWarning("LoadAdPlacementsFromFile received null or empty file content.");
                return BadRequest("File content cannot be null or empty.");
            }

            try
            {
                var adPlacements = ParseAdPlacements(fileContent);
                var newAdPlacements = new ConcurrentDictionary<string, AdPlacement>(StringComparer.OrdinalIgnoreCase);

                foreach (var placement in adPlacements)
                {
                    if (!string.IsNullOrEmpty(placement.Name))
                    {
                        newAdPlacements.TryAdd(placement.Name, placement);
                    }
                }

                _adPlacementsByName = newAdPlacements;
                _logger.LogInformation($"Successfully loaded {adPlacements.Count} ad placements."); //Log at success
                return Ok($"Successfully loaded {adPlacements.Count} ad placements from file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ad placements."); //Detailed error log
                return StatusCode(500, $"Error loading ad placements: {ex.Message}");
            }
        }


        private List<AdPlacement> ParseAdPlacements(string fileContent)
        {
            var placements = new List<AdPlacement>();
            using (StringReader reader = new StringReader(fileContent))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();
                        string locationsString = parts[1].Trim();
                        string[] locations = locationsString.Split(',').Select(s => s.Trim()).ToArray();

                        var placement = new AdPlacement
                        {
                            Name = name,
                            Locations = locations.ToList()
                        };
                        placements.Add(placement);
                    }
                    else
                    {
                        _logger.LogWarning($"Skipping invalid line: {line}"); //Log bad data
                        Console.WriteLine($"Skipping invalid line: {line}");
                    }
                }
            }
            return placements;
        }

        [HttpGet("Search")]
        public IActionResult SearchAdPlacements(string location)
        {
            _logger.LogInformation($"SearchAdPlacements endpoint called with location: {location}"); //Log parameters
            if (string.IsNullOrEmpty(location))
            {
                _logger.LogWarning("SearchAdPlacements received null or empty location parameter.");
                return BadRequest("Location parameter is required.");
            }

            try
            {
                var matchingPlacements = new List<AdPlacement>();

                foreach (var placement in _adPlacementsByName.Values)
                {
                    if (IsPlacementActiveInLocation(placement, location))
                    {
                        matchingPlacements.Add(placement);
                    }
                }

                _logger.LogInformation($"Found {matchingPlacements.Count} matching placements for location: {location}.");
                return Ok(matchingPlacements.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching ad placements for location: {location}.");
                return StatusCode(500, $"Error searching ad placements: {ex.Message}");
            }
        }

        private bool IsPlacementActiveInLocation(AdPlacement placement, string location)
        {
            foreach (string placementLocation in placement.Locations)
            {
                if (location.StartsWith(placementLocation))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
