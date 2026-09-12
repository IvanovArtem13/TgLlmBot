using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpConfiguration
{
    private McpConfiguration(
        McpGithubConfiguration? github,
        McpExaConfiguration? exa)
    {
        Github = github;
        Exa = exa;
    }

    public McpGithubConfiguration? Github { get; }

    public McpExaConfiguration? Exa { get; }

    public static McpConfiguration Convert(McpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var github = McpGithubConfiguration.Convert(options.Github);
        var exa = McpExaConfiguration.Convert(options.Exa);
        return new(github, exa);
    }
}
