using System;

namespace TgLlmBot.Services.Mcp.Clients.Github;

public class DefaultGithubMcpClientFactoryOptions
{
    public DefaultGithubMcpClientFactoryOptions(Uri endpoint, string githubPat)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (string.IsNullOrEmpty(githubPat))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(githubPat));
        }

        Endpoint = endpoint;
        GithubPat = githubPat;
    }

    public Uri Endpoint { get; }
    public string GithubPat { get; }
}
