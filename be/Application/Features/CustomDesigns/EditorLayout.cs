using System.Text.Json;
using System.Text.Json.Serialization;

namespace be.Application.Features.CustomDesigns;

// Version 1 saved designs remain readable. New layouts have a bounded typed
// contract; no HTML, script, external URL or browser pricing is interpreted.
public sealed class EditorLayout
{
    public int SchemaVersion { get; set; } = 2;
    public string ArtworkFit { get; set; } = "contain";
    public decimal ArtworkScale { get; set; } = 1;
    public decimal ArtworkX { get; set; } = 50;
    public decimal ArtworkY { get; set; } = 50;
    public decimal ArtworkRotation { get; set; }
    public decimal TextX { get; set; } = 50;
    public decimal TextY { get; set; } = 50;
    public decimal TextSize { get; set; } = 12;
    public string Background { get; set; } = "white";

    public static int Version(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 1;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("schemaVersion", out var version) && version.TryGetInt32(out var number) ? number : 1;
    }

    public static void Validate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("Editor state must be an object.");
        if (!document.RootElement.TryGetProperty("schemaVersion", out var version)) return; // Historical opaque layouts.
        if (!version.TryGetInt32(out var number) || number is < 1 or > 2) throw new JsonException("Unknown editor schema.");
        if (number == 1) return;
        var layout = JsonSerializer.Deserialize<EditorLayout>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }) ?? throw new JsonException();
        if (layout.ArtworkFit is not ("contain" or "cover") || layout.Background is not ("white" or "transparent") ||
            layout.ArtworkScale is < .25m or > 4m || layout.ArtworkRotation is < -180 or > 180 || layout.TextSize is < 2 or > 40 ||
            new[] { layout.ArtworkX, layout.ArtworkY, layout.TextX, layout.TextY }.Any(value => value is < 0 or > 100))
            throw new JsonException("Editor layout is outside supported limits.");
    }
}
