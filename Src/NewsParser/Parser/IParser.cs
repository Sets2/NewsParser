using NewsParser.Core.Domain;

namespace NewsParser.Parser;

public interface IParser
{
    public List<Item> Parse(string content);
}