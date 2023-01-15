using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NewsParser.Core.Domain;

public class Item
{
    public long Id { get; set; }
    //Ссылка на страницу сообщения в интернете.
    public string? Link { get; set; }
    //Краткий обзор сообщения.
    public string? Title { get; set; }
    //Краткое содержание новости.
    public string? Description { get; set; }
    //Дата публикации сообщения.
    public string? PubDate { get; set; }
    //Расширенное содержание новости.
    public string? Summary { get; set; }
    public bool IsReaded { get; set; }=false;
    public long ChannelId { get; set; }

    public virtual Channel Channel { get; set; } = null!;

    public bool IsSaved = false;

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        Item? objAsItem = obj as Item;
        if (objAsItem == null) return false;
        else return Equals(objAsItem);
    }

    public bool Equals(Item other)
    {
        if (other == null) return false;
        return (this.Id.Equals(other.Id));
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}