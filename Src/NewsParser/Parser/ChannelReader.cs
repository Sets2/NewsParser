using System.Runtime.CompilerServices;
using System.Text;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NewsParser.Core.Domain;

namespace NewsParser.Parser;

public class ChannelReader
{
    // Сущность новостного канала
    private readonly Channel _channel;
    private readonly IParser _parser;
    private readonly HttpClient _httpClient;
    private readonly DataContext _dataContext;
    private readonly IOptions<ParserSettings> _options;
    private readonly ILogger<GroupChannels> _logger;

    // Очередь из последних считанных ссылок из запроса на чтение канала
    // необходима для предотвращения повторных попыток записи в БД
    private List<Item> _bufferItems;

    // Успешность выполнения последнего запроса на чтение канала
    private bool _IsSuccessRead = true;

    // Последнее время выполнения запроса на чтение канала
    private DateTime _lasttime;

    public ChannelReader(Channel channel, IParser parser, HttpClient httpClient, DataContext dataContext,
        IOptions<ParserSettings> options, ILogger<GroupChannels> logger)
    {
        _channel=channel;
        _parser = parser;
        _httpClient = httpClient;
        _dataContext = dataContext;
        _options = options;
        _logger = logger;
        _bufferItems = new List<Item>(_options.Value.bufferItemsMax * 2);
        _lasttime = DateTime.UtcNow.AddSeconds(-_options.Value.timeReadItemError * 2);
    }
    public long Id
    {
        get => _channel.Id;
    }
    public async Task<string?> ReadNewsSite()
    {
        _IsSuccessRead = false;
        _lasttime = DateTime.UtcNow;
        string? content = null;
        HttpRequestMessage? request = null;
        HttpResponseMessage? response = null;
        try
        {
            request = new HttpRequestMessage(HttpMethod.Get, _channel.Link);
            //request.Headers.Add("Accept", "text/xml,text/html,application/xhtml+xml,application/xml");
            request.Headers.Add("Accept-Charset", "UTF-8");
            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; rv:77.0) Gecko/20100101 Firefox/77.0");
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            if (response.IsSuccessStatusCode)
            {
                var contentByte = await response.Content.ReadAsByteArrayAsync();
                if (response.Content.Headers.ContentType?.CharSet?.Contains("1251") == true)
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    content = Encoding.GetEncoding(1251).GetString(contentByte);
                }
                else content = Encoding.UTF8.GetString(contentByte);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message +$" Возникла ошибка при получении данных с новостного сервера по каналу {_channel.Id} {_channel.Title}");
        }
        finally
        {
            response?.Dispose();
            request?.Dispose();
            if (content != null)
            {
                _IsSuccessRead=true;
            }
        }
        return content;
    }

    public async Task ReadParse()
    {
        string? content= null;
        // Прошло секунд после последнего опроса
        var deltaTime = (DateTime.UtcNow - _lasttime).TotalSeconds;
        if ((_IsSuccessRead & deltaTime > _options.Value.timeReadItemSuccess) |
            (!_IsSuccessRead & deltaTime > _options.Value.timeReadItemError))
        {
            content = await ReadNewsSite();
            if (content != null)
            {
                var itemsList = _parser.Parse(content);
                itemsList.ForEach(x=> x.ChannelId = _channel.Id);
                _bufferItems = _bufferItems.UnionBy(itemsList, x => x.Link).ToList();
                await SaveBuffersItems();
            }
        }
    }

    public async Task SaveBuffersItems()
    {
        // Выбираем несохраненные  элементы
        var notSaved = _bufferItems.FindAll(x => x.IsSaved == false);
        if(notSaved.Any())
        {
            try
            {
                await _dataContext.Item.AddRangeAsync(notSaved);
                await _dataContext.SaveChangesAsync();
                // Выставляем признак сохраненности
                notSaved.ForEach(x=> x.IsSaved=true);
                // Осталяем последние элементы, не превышающие допустимого количества
                var countDelItems = _bufferItems.Count - _options.Value.bufferItemsMax;
                if(countDelItems > 0) 
                    _bufferItems = _bufferItems.GetRange(countDelItems, _bufferItems.Count- countDelItems);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{e.Message} Ошибка записи Item в БД");
            }
        }
    }

    public async Task InitBuffersItems()
    {
        try
        {
            // Считываем для буферизации последние элементы, не превышающие допустимого количества
            var _initBufferItems = await _dataContext.Item.Where(x=>x.ChannelId==_channel.Id).
                OrderByDescending(x=>x.Id).
                Take(_options.Value.bufferItemsMax).
                AsNoTracking().ToListAsync();
            // Выставляем признак сохраненности
            _initBufferItems.ForEach(x => x.IsSaved = true);
            _bufferItems.AddRange(_initBufferItems);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"{e.Message} Ошибка чтения Item из БД при инициализации");
        }
    }

    public void UpdateLink(Channel channel)
    {
        if (_channel.Link != channel.Link) _channel.Link = channel.Link;
    }

    private void AddItemsToBuffer(List<Item> itemsList)
    {
        var existItems=itemsList.
            Join(_bufferItems,x=> x.Link,z=>z.Link, (x,z) => x.Link);
        itemsList.RemoveAll(x=>existItems.Contains(x.Link));
        _bufferItems.AddRange(itemsList);
    }

}