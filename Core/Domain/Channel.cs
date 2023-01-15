using NewsParser.Parser;

namespace NewsParser.Core.Domain;

public class Channel
{
    public long Id { get; set; }
    //Ссылка на сайт источника
    public string? Link { get; set; }
    //Заголовок сайта - источника
    public string? Title { get; set; }
    //Описание сайта - источника
    public string? Description { get; set; }
    public virtual ICollection<Item>? Items { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        Channel? objAsChannel = obj as Channel;
        if (objAsChannel == null) return false;
        else return Equals(objAsChannel);
    }
    public bool Equals(Channel other)
    {
        if (other == null) return false;
        return (this.Id.Equals(other.Id));
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

}