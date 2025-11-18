using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ZAShinyWarper;

public class LocationParser
{
    public static readonly Dictionary<string, Vector3>? LysandreSpawnerCoordinates = ParseLocationData(GetEmbeddedResource("SpawnerLocations.LysandreLabs.json") ?? "[]");
    public static readonly Dictionary<string, Vector3>? MainSpawnerCoordinates = ParseLocationData(GetEmbeddedResource("SpawnerLocations.Main.json") ?? "[]");
    public static readonly Dictionary<string, Vector3>? SewersSpawnerCoordinates = ParseLocationData(GetEmbeddedResource("SpawnerLocations.Sewers.json") ?? "[]");

    private static string? GetEmbeddedResource(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = $"{assembly.GetName().Name}.Resources.{resourceName}";

            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                Console.WriteLine($"Resource not found: {resourcePath}");
                return null;
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading embedded resource: {ex.Message}");
            return null;
        }
    }

    private static Dictionary<string, Vector3>? ParseLocationData(string json)
    {
        try
        {
            var locationStrings = JsonConvert.DeserializeObject<List<string>>(json);
            if (locationStrings == null) return null;

            var coordinates = new Dictionary<string, Vector3>();
            var regex = new Regex(@"V3f\(([-\d., ]+)\)");

            foreach (var entry in locationStrings)
            {
                var parts = entry.Split([" - "], StringSplitOptions.None);
                if (parts.Length < 3) continue;

                var hash = parts[1];
                var match = regex.Match(entry);

                if (match.Success)
                {
                    var coordParts = match.Groups[1].Value.Split(',');
                    if (coordParts.Length == 3)
                    {
                        var x = float.Parse(coordParts[0].Trim());
                        var y = float.Parse(coordParts[1].Trim());
                        var z = float.Parse(coordParts[2].Trim());

                        coordinates[hash] = new Vector3 { X = x, Y = y, Z = z };
                    }
                }
            }

            return coordinates;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing location data: {ex.Message}");
            return null;
        }
    }
}