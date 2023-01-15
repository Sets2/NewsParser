namespace NewsParser.Parser;

public interface IGroupChannels
{
    public Task UpdateChannels();
    public Task ReadChannels();
}