using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Mcp;

public class McpOptions
{
    public McpGithubOptions? Github { get; set; }

    public McpExaOptions? Exa { get; set; }
}
