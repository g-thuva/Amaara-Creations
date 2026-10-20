namespace be.Api.Configuration;

public sealed class ReleaseControls
{
    public bool AcceptNewPayments { get; set; } = true;
    public bool AcceptUploads { get; set; } = true;
    public bool AllowAdminChanges { get; set; } = true;
}
