using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Llm;

public class LlmOptions
{
    [Required]
    [MaxLength(10000)]
    [Url]
    public string Endpoint { get; set; } = default!;

    [Required]
    [MaxLength(10000)]
    public string ApiKey { get; set; } = default!;

    [Required]
    [MaxLength(10000)]
    public string Model { get; set; } = default!;

    [Required]
    [MaxLength(10000)]
    public string DefaultResponse { get; set; } = default!;

    /// <summary>
    ///     Капабилити модели. Не обязательны: без секции модель считается чисто текстовой.
    /// </summary>
    [Required]
    public LlmCapabilitiesOptions Capabilities { get; set; } = new();
}
