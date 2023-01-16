using NewsParser;
using System.IO;
using System.Text.Json;
using NewsParser.Core.Domain;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DataAccess.Data
{
    public class DbInitializer: IDbInitializer
    {
        private readonly DataContext _dataContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(DataContext dataContext, IConfiguration configuration, ILogger<DbInitializer> logger)
        {
            _dataContext = dataContext;
            _configuration = configuration;
            _logger = logger;
        }
        
        public async Task InitializeDb()
        {
            const string channelFile = "dbInitChannel.json";
            const string itemFile = "dbInitChannelItems.json";

//            await _dataContext.Database.EnsureDeletedAsync();
            if (await _dataContext.Database.EnsureCreatedAsync())
            {
                var path = _configuration["DbInit"];

                var dir = Directory.GetCurrentDirectory();
                _logger.LogInformation("Текущий каталог " + dir);
                _logger.LogInformation("Существует каталог инициализации " + path + " :" + Directory.Exists(path));
                if (path == null)
                {
                    _logger.LogWarning(
                        "Отсутствует параметер DbInit в appsetting.json. Инициализация не проведена");
                    return;
                }
                if (!File.Exists(path + channelFile) | !File.Exists(path + itemFile))
                {
                    _logger.LogWarning(
                        "Отсутствуют файлы для инициализации. Инициализация не проведена");
                    return;
                }
                try
                {
                    string? jsonString = File.ReadAllText(path + channelFile);
                    if (jsonString?.Length > 1)
                    {
                        var listChannel = JsonSerializer.Deserialize<List<Channel>>(jsonString);
                        if (listChannel != null)
                        {
                            await _dataContext.Channel.AddRangeAsync(listChannel);
                            await _dataContext.SaveChangesAsync();
                }
                    }

                    jsonString = File.ReadAllText(path+itemFile);
                    if (jsonString?.Length > 1)
                    {
                        var listItem = JsonSerializer.Deserialize<List<Item>>(jsonString);
                        if (listItem != null)
                        {
                            await _dataContext.Item.AddRangeAsync(listItem);
                            await _dataContext.SaveChangesAsync();
                        }
                    }
                    _logger.LogInformation("Проведена инициализация из json файлов");

                }
                catch (Exception e)
                {
                    _logger.LogError(e,e.Message+" Ошибка преобразования и записи данных для инициализации из json файлов");
                }
            }
        }
    }
}