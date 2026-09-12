using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpExaConfiguration
{
    private McpExaConfiguration(Uri endpoint, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(apiKey);
        Endpoint = endpoint;
        ApiKey = apiKey;
    }

    public Uri Endpoint { get; }

    public string ApiKey { get; }

    public static McpExaConfiguration? Convert(McpExaOptions? options)
    {
        if (options is null || !options.Enabled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException(
                "The Exa MCP is enabled but no API key is configured. "
                + "Set Mcp:Exa:ApiKey (e.g. via User Secrets) or disable Mcp:Exa:Enabled.",
                nameof(options));
        }

        var endpoint = new Uri(options.Endpoint, UriKind.Absolute);
        return new(endpoint, options.ApiKey);
    }
}
