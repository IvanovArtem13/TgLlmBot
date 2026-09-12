using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using TgLlmBot.Services.Mcp.Enums;

namespace TgLlmBot.Services.Mcp.Clients.Exa;

public class DefaultExaMcpClientFactory : IExaMcpClientFactory
{
    public const string ExaHttpClientName = $"http-client-factory-{nameof(McpClientName.Exa)}";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    private readonly DefaultExaMcpClientFactoryOptions _options;

    public DefaultExaMcpClientFactory(
        DefaultExaMcpClientFactoryOptions options,
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _options = options;
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public async Task<McpClient> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var httpClient = _httpClientFactory.CreateClient(ExaHttpClientName);
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = _options.Endpoint,
                Name = nameof(McpClientName.Exa),
                AdditionalHeaders = new Dictionary<string, string>
                {
                    { "x-api-key", _options.ApiKey }
                }
            },
            httpClient,
            _loggerFactory,
            ownsHttpClient: false);
        return await McpClient.CreateAsync(
            transport,
            null,
            _loggerFactory,
            cancellationToken);
    }
}
