namespace NewsParser.Parser;

public class ParserSettings
{
    // Максимальная длина очереди из последних считанных ссылок до которой будет обрезаться
    // после сохранения в БД
    public int bufferItemsMax { get; set; }

    // Диапазон времени до следующего запроса после успешного запроса на чтение канала
    public int timeReadItemSuccess { get; set; }

    // Диапазон времени до следующего запроса после неудачного запроса на чтение канала
    public int timeReadItemError { get; set; }

    // Диапазон времени обновления данных о каналах из таблицы БД Channel в GropupReader
    public int timeChannelsUpdate { get; set; }

    // Диапазон времени ожидания перед повторной обработкой группы каналов в ParseHostedServices
    public int timeWaitHostedServices { get; set; }

}