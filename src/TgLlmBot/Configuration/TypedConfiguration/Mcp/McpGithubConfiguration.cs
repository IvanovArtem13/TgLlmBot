using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpGithubConfiguration
{
    private McpGithubConfiguration(Uri endpoint, string personalAccessToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(personalAccessToken);
        Endpoint = endpoint;
        PersonalAccessToken = personalAccessToken;
    }

    public Uri Endpoint { get; }

    public string PersonalAccessToken { get; }

    public static McpGithubConfiguration? Convert(McpGithubOptions? options)
    {
        if (options is null || !options.Enabled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.PersonalAccessToken))
        {
            throw new ArgumentException(
                "The Github MCP is enabled but no personal access token is configured. "
                + "Set Mcp:Github:PersonalAccessToken (e.g. via User Secrets) or disable Mcp:Github:Enabled.",
                nameof(options));
        }

        var endpoint = new Uri(options.Endpoint, UriKind.Absolute);
        return new(endpoint, options.PersonalAccessToken);
    }
}
