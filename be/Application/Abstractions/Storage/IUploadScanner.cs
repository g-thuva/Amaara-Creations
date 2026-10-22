namespace be.Application.Abstractions.Storage;
public interface IUploadScanner
{
    Task ScanAsync(byte[] image, CancellationToken cancellationToken);
}
