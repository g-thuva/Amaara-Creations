using be.Application.Features.Identity;
namespace be.Tests;
public sealed class AvatarMediaUrlTests
{
    [Theory]
    [InlineData("/api/v1/media/public/avatars/11111111111111111111111111111111.png", true)]
    [InlineData("/uploads/avatars/legacy-owner_11111111-1111-1111-1111-111111111111.png", true)]
    [InlineData("https://cdn.example.test/avatar.png", true)]
    [InlineData("s3:private/custom-artwork/1/file.png", false)]
    [InlineData("//other.example.test/avatar.png", false)]
    [InlineData("/api/v1/media/public/../private/file.png", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://s3.example.test/file?X-Amz-Signature=sensitive", false)]
    public void AcceptsPortablePublicReferencesWithoutAcceptingPrivateGrants(string url, bool valid) => Assert.Equal(valid, new AvatarMediaUrlAttribute().IsValid(url));
}
