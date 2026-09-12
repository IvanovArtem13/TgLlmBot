using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Mcp;

public class McpExaOptions
{
    public bool Enabled { get; set; } = true;

    [Required]
    [MaxLength(2000)]
    public string Endpoint { get; set; } = "https://mcp.exa.ai/mcp";

    [MaxLength(500)]
    public string? ApiKey { get; set; }
}
