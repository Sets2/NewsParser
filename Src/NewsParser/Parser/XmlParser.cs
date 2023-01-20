using NewsParser.Core.Domain;
using System.Xml;

namespace NewsParser.Parser;

public class XmlParser:IParser
{
    private readonly XmlDocument _xDoc = new XmlDocument();
    private readonly ILogger<XmlParser> _logger;

    public XmlParser(ILogger<XmlParser> logger)
    {
        _logger = logger;
    }

    public List<Item> Parse(string content)
    {
        List<Item> ItemsList = new List<Item>();
        string tagname;

        try
        {
            _xDoc.LoadXml(content);
            XmlElement? xRoot = _xDoc.DocumentElement;
            if (xRoot != null)
            {
                XmlNodeList? nodes = xRoot.SelectNodes("//item");
                if (nodes != null)
                {
                    foreach (XmlElement xnode in nodes)
                    {
                        Item item = new Item();
                        foreach (XmlNode childnode in xnode.ChildNodes)
                        {
                            tagname = childnode.Name;
                            if (tagname == "title") item.Title = childnode.InnerText;
                            if (tagname == "link") item.Link = childnode.InnerText;
                            if (tagname == "pubDate") item.PubDate = childnode.InnerText;
                            if (tagname == "description") item.Description = childnode.InnerText;
                            //if (tagname.Contains("full-text") | tagname.Contains("content:"))
                            //    item.Summary = childnode.InnerText; // Большой объем текста - закомментированно для отладки
                        }

                        ItemsList.Add(item);
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,$"{e.Message} Ошибка парсинга XML контента в коллекцию");
        }

        return ItemsList;
    }
}