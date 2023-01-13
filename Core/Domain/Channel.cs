namespace NewsParser.Core.Domain;

public class Channel
{
    public Guid Id { get; set; }
    //Заголовок сайта - источника
    public string? Title { get; set; }
    //Описание сайта - источника
    public string? Desc { get; set; }
    //Ссылка на сайт источника
    public string? Link { get; set; }

    public virtual ICollection<Items>? Items { get; set; }

}