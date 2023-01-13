using Microsoft.VisualBasic;
using System.Diagnostics;

namespace NewsParser.Core.Domain;

public class Items
{
    public Guid Id { get; set; }
    //Заголовок сообщения.
    public string? Title { get; set; }
    //Ссылка на страницу сообщения в интернете.
    public string? Link { get; set; }
    //Краткий обзор сообщения.
    public string? Desc { get; set; }
    //Дата публикации сообщения.
    public string? PubDate { get; set; }
    public Guid ChannelId { get; set; }
    public virtual Channel Channel { get; set; } = null!;

}