using System.Linq;
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

    // Уточненный максимальный размер очереди канала
    private int _bufferItemsMax;

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
        _bufferItemsMax=_options.Value.bufferItemsMax;
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

    public async Task ReadParseAndSaveBuffers()
    {
        await ReadParse();
        await SaveBuffersItems();
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
                //var buferItemsLink = _bufferItems.Select(x=>x.Link);
                //var exceptItems = itemsList.ExceptBy(buferItemsLink, x => x.Link).ToList();
                //_bufferItems.AddRange(exceptItems);
            }
        }
    }

    public async Task SaveBuffersItems()
    {
        bool isSuccess = true;
        // Выбираем несохраненные  элементы
        var notSaved = _bufferItems.FindAll(x => x.IsSaved == false);
        if(notSaved.Any())
        {
            foreach (var item in notSaved)
            {
                try
                {
                    //var exItem = _dataContext.Item.Local.SingleOrDefault(o => o.Link == item.Link);
                    //if (exItem != null)
                    //    _dataContext.Entry(exItem).State = EntityState.Detached;

                    var addedItem = await _dataContext.Item.AddAsync(item);
                    await _dataContext.SaveChangesAsync();
                    item.IsSaved = true;
                    _dataContext.Entry(item).State = EntityState.Detached;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{e.Message} Ошибка записи Item в БД {item.Id} {item.Link} ");
                    isSuccess = false;
                    _dataContext.Entry(item).State = EntityState.Detached;
                }
            }
            // Выбираем несохраненные элементы из за ошибки записи (дубликат ссылки)
            if (!isSuccess)
            {
                notSaved = notSaved.FindAll(x => x.IsSaved == false);
                if (notSaved.Any()) await UpdateStatusAndId(notSaved);
            }

            // Осталяем последние элементы, не превышающие допустимого количества
            var countDelItems = _bufferItems.Count - _bufferItemsMax;
            if (countDelItems > 0)
                _bufferItems = _bufferItems.GetRange(countDelItems, _bufferItems.Count - countDelItems);
        }
    }

    private async Task UpdateStatusAndId(IEnumerable<Item> items)
    {
        var linkItems = items.Select(x => x.Link);
        List<Item> efItems=null;
        try
        {
            efItems = await _dataContext.Item.Where(x => linkItems.Contains(x.Link)).
                AsNoTracking().ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"{e.Message} Ошибка чтения Item из БД");
            return;
        }
        var comList = items.
            Join(efItems,x=>x.Link,y=>y.Link,(x,y)=> new{x,y.Id}).ToList();
        foreach (var item in comList)
        {
            item.x.Id = item.Id;
            item.x.IsSaved = true;
        }
    }

    public async Task InitBuffersItems()
    {
        try
        {
            // Тестовое считывание канала для настройки размера очереди буферизации
            await ReadParse();
            if (_bufferItems.Count * 2 > _bufferItemsMax)
            {
                _bufferItemsMax = _bufferItems.Count * 2;
                _bufferItems.Capacity = _bufferItemsMax * 2;
            }
            _bufferItems.Clear();
            // Считываем для буферизации последние элементы, не превышающие допустимого количества
            var _initBufferItems = await _dataContext.Item.
                Where(x=>x.ChannelId==_channel.Id).
                OrderByDescending(x=>x.Id).
                Take(_bufferItemsMax).
                AsNoTracking().OrderBy(x=>x.Id).
                ToListAsync();
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