namespace namera_API.Common.Exceptions;

public sealed class ApiException : Exception
{
    public ApiException(string message, IReadOnlyList<string>? errors = null) : base(message)
    {
        Errors = errors ?? [];
    }

    public IReadOnlyList<string> Errors { get; }
}
