using System.Text.Json;
using be.Application.Features.CustomDesigns;

namespace be.Tests;

public sealed class EditorLayoutTests
{
    [Theory]
    [InlineData("{\"schemaVersion\":2,\"artworkScale\":100}")]
    [InlineData("{\"schemaVersion\":2,\"artworkX\":-1}")]
    [InlineData("{\"schemaVersion\":2,\"artworkY\":101}")]
    [InlineData("{\"schemaVersion\":2,\"artworkRotation\":999}")]
    [InlineData("{\"schemaVersion\":2,\"textSize\":0}")]
    [InlineData("{\"schemaVersion\":2,\"artworkFit\":\"url(javascript:alert(1))\"}")]
    [InlineData("{\"schemaVersion\":2,\"background\":\"red\"}")]
    [InlineData("{\"schemaVersion\":2,\"externalUrl\":\"https://attacker.example\"}")]
    [InlineData("{\"schemaVersion\":99}")]
    [InlineData("[]")]
    public void RejectsUnboundedOrActiveNewEditorState(string json) => Assert.Throws<JsonException>(() => EditorLayout.Validate(json));

    [Fact]
    public void KeepsLegacyLayoutsAndAcceptsBoundedVersionTwo()
    {
        EditorLayout.Validate("{}");
        EditorLayout.Validate("{\"schemaVersion\":1,\"preview\":{\"text\":\"Legacy\"}}");
        EditorLayout.Validate("{\"schemaVersion\":2,\"artworkScale\":1.5,\"artworkRotation\":90,\"background\":\"transparent\"}");
        Assert.Equal(2, EditorLayout.Version("{\"schemaVersion\":2}"));
        Assert.Equal(1, EditorLayout.Version("{}"));
    }
}
