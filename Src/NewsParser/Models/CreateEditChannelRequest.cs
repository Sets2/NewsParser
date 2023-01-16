namespace NewsParser.Models;

/// <example>
///{
///    "Link": "https://www.vedomosti.ru/newspaper/out/rss.xml"
///    "Title": "Ведомости. Ежедневная деловая газета"
///    "Description": "Ведомости. Новости, 14.01.2023"
///}
/// </example>

public class CreateEditChannelRequest
{
    public string Link { get; set; } = null!;
    public string? Title { get; set; }
    public string? Description { get; set; }
}