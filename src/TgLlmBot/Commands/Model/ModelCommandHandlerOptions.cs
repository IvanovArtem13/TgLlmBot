using System;

namespace TgLlmBot.Commands.Model;

public class ModelCommandHandlerOptions
{
    public ModelCommandHandlerOptions(Uri endpoint, string model, bool image, bool video, bool audio)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(model));
        }

        Endpoint = endpoint;
        Model = model;
        Image = image;
        Video = video;
        Audio = audio;
    }

    public Uri Endpoint { get; }

    public string Model { get; }

    public bool Image { get; }

    public bool Video { get; }

    public bool Audio { get; }
}
