namespace be.Application.Common;
public sealed record DownloadContent(Stream Content, string ContentType, string FileName, bool EnableRangeProcessing = false)
{
    public DownloadContent(byte[] content, string contentType, string fileName) : this(new MemoryStream(content, writable: false), contentType, fileName)
    {
    }
}
