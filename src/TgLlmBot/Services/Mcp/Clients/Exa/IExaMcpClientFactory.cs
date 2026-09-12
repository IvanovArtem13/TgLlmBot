using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace TgLlmBot.Services.Mcp.Clients.Exa;

public interface IExaMcpClientFactory
{
    Task<McpClient> CreateAsync(CancellationToken cancellationToken);
}
