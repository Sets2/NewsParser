using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NewsParser.Core.Domain;

namespace NewsParser.Parser;

public class GroupChannels:IGroupChannels
{
    //private const string _uri = "https://rssexport.rbc.ru/rbcnews/news/20/full.rss";
    //private const string _uri = "https://lenta.ru/rss/google-newsstand/main/";
    //private const string _uri = "https://www.kommersant.ru/RSS/news.xml";
    //private const string _uri = "http://www.vedomosti.ru/newspaper/out/rss.xml";
    //private const string _uri = "http:https://www.vedomosti.ru/rss/articles";
    //private const string _uri = "https://eadaily.com/ru/rss/index.xml";

    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _service;
    private readonly IOptions<ParserSettings> _options;
    private readonly ILogger<GroupChannels> _logger;
    private readonly DataContext _dataContext;
    private List<ChannelReader> _channelReaders = new List<ChannelReader>();

    // Последнее время выполнение обновления списка каналов
    private DateTime _lasttime;

    public GroupChannels(IServiceProvider service, IOptions<ParserSettings> options, ILogger<GroupChannels> logger)
    {
        _httpClient = new HttpClient();
        _service = service;
        _options = options;
        _logger = logger;
        var scope = service.CreateScope();
        _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        _dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        _lasttime = DateTime.UtcNow.AddSeconds(-_options.Value.timeChannelsUpdate * 2);
    }

    public async Task UpdateChannels()
    {
        var deltaTime = (DateTime.UtcNow - _lasttime).TotalSeconds;
        if (deltaTime > _options.Value.timeChannelsUpdate)
        {
            _lasttime = DateTime.UtcNow;
            try
            {
                var channelReadersId = _channelReaders.Select(x => x.Id);
                List<Channel> currChannels = await _dataContext.Channel.AsNoTracking().ToListAsync();

                bool exist;
                // Удалить отсутствующие в БД каналы и обновить существующие
                foreach (var channel in _channelReaders)
                {
                    exist = false;
                    foreach (var currChannel in currChannels)
                    {
                        if (channel.Id == currChannel.Id) 
                        {
                            channel.UpdateLink(currChannel);
                            exist = true;
                            break;
                        }
                    }

                    if (!exist) _channelReaders.Remove(channel);
                }

                // Добавить из БД новые каналы
                var newChannels = currChannels.FindAll(x => !channelReadersId.Contains(x.Id));
                foreach (var newChannel in newChannels)
                {
                    var prs = _service.GetRequiredService<IParser>();
                    var channelReader = new ChannelReader(newChannel, prs, _httpClient, _dataContext, _options, _logger);
                    await channelReader.InitBuffersItems(); // Инициализация буфера последними записями из БД
                    _channelReaders.Add(channelReader);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"{e.Message} Ошибка считывания и обновления данных каналов из БД");
            }
        }
    }

    public async Task ReadChannels()
    {
        foreach (var reader in _channelReaders)
        {
            await reader.ReadParse();
        }
    }
}