namespace MyPortfolioAPI.Exceptions;

public sealed class ClientSafeException : Exception
{
    public ClientSafeException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
