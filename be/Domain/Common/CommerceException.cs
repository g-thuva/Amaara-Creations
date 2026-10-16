namespace be.Domain.Common;
public sealed class CommerceException(string code, string message, int status = 400, IReadOnlyDictionary<string, object?>? details = null) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
    public IReadOnlyDictionary<string, object?> Details { get; } = details ?? new Dictionary<string, object?>();
}
