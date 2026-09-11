using System;
using TgLlmBot.Configuration.Options.Llm;
using TgLlmBot.DataAccess.Models;
using TgLlmBot.Services.Media;

namespace TgLlmBot.Configuration.TypedConfiguration.Llm;

/// <summary>
///     Капабилити основной модели: какие виды вложений она видит сама. Отдельной vision-модели
///     нет - вложения обрабатывает та же модель, что и текст, если её капабилити это позволяют.
/// </summary>
public sealed class LlmCapabilitiesConfiguration
{
    private LlmCapabilitiesConfiguration(bool image, bool video, bool audio)
    {
        Image = image;
        Video = video;
        Audio = audio;
    }

    public bool Image { get; }

    public bool Video { get; }

    public bool Audio { get; }

    public static LlmCapabilitiesConfiguration Convert(LlmCapabilitiesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new(options.Image, options.Video, options.Audio);
    }

    /// <summary>
    ///     Видит ли модель подготовленное вложение такого вида: картинка требует
    ///     <see cref="Image" />, файл видео и цепочка отрендеренных кадров - <see cref="Video" />.
    /// </summary>
    public bool Supports(PreparedMediaKind kind)
    {
        return kind switch
        {
            PreparedMediaKind.Image => Image,
            PreparedMediaKind.VideoFile or PreparedMediaKind.RenderedFrames => Video,
            _ => false
        };
    }

    /// <summary>
    ///     Видит ли модель вложение такого вида из чата - известно ещё до скачивания файла
    ///     и позволяет не качать то, что показать модели всё равно не получится.
    /// </summary>
    public bool Supports(DbMediaKind kind, bool isAnimated)
    {
        return kind switch
        {
            DbMediaKind.Photo => Image,
            DbMediaKind.Sticker => isAnimated ? Video : Image,
            DbMediaKind.Animation or DbMediaKind.Video => Video,
            _ => false
        };
    }
}
