namespace be.Application.Common;
public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("Validation failed")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

internal sealed class RequestValidation
{
    private readonly Dictionary<string, List<string>> errors = new();
    public bool IsValid => errors.Count == 0;

    public void AddModelError(string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
            errors[key] = messages = [];
        messages.Add(message);
    }

    public RequestValidationException Exception() => new(errors.ToDictionary(p => p.Key, p => p.Value.ToArray()));
}
