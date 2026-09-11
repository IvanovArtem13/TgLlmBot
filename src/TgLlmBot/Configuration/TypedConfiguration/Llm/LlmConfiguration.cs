using System;
using TgLlmBot.Configuration.Options.Llm;

namespace TgLlmBot.Configuration.TypedConfiguration.Llm;

public class LlmConfiguration
{
    private LlmConfiguration(
        Uri endpoint,
        string apiKey,
        string model,
        string defaultResponse,
        LlmCapabilitiesConfiguration capabilities)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(capabilities);
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(apiKey));
        }

        if (string.IsNullOrEmpty(model))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(model));
        }

        if (string.IsNullOrEmpty(defaultResponse))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(defaultResponse));
        }

        Endpoint = endpoint;
        ApiKey = apiKey;
        Model = model;
        DefaultResponse = defaultResponse;
        Capabilities = capabilities;
    }

    public Uri Endpoint { get; }
    public string ApiKey { get; }
    public string Model { get; }
    public string DefaultResponse { get; }

    /// <summary>
    ///     Капабилити модели: какие вложения она видит сама. Одна модель на всё - и на текст,
    ///     и на вложения, если капабилити позволяют.
    /// </summary>
    public LlmCapabilitiesConfiguration Capabilities { get; }

    public static LlmConfiguration Convert(LlmOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var typedEndpoint))
        {
            throw new ArgumentException("Invalid endpoint.", nameof(options));
        }

        var capabilities = LlmCapabilitiesConfiguration.Convert(options.Capabilities);
        return new(
            typedEndpoint,
            options.ApiKey,
            options.Model,
            options.DefaultResponse,
            capabilities);
    }
}
