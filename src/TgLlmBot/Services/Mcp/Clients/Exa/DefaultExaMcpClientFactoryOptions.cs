using System;

namespace TgLlmBot.Services.Mcp.Clients.Exa;

public class DefaultExaMcpClientFactoryOptions
{
    public DefaultExaMcpClientFactoryOptions(Uri endpoint, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(apiKey));
        }

        Endpoint = endpoint;
        ApiKey = apiKey;
    }

    public Uri Endpoint { get; }
    public string ApiKey { get; }
}
