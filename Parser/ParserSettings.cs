namespace NewsParser.Parser;

public class ParserSettings
{
    // Максимальная длина очереди из последних считанных ссылок до которой будет обрезаться
    // после сохранения в БД
    public int bufferItemsMax = 100;

    // Диапазон времени до следующего запроса после успешного запроса на чтение канала
    public int timeReadItemSuccess = 60;

    // Диапазон времени до следующего запроса после неудачного запроса на чтение канала
    public int timeReadItemError = 60;

    // Диапазон времени обновления данных о каналах из таблицы БД Channel в GropupReader
    public int timeChannelsUpdate = 60;

    // Диапазон времени ожидания перед повторной обработкой группы каналов в ParseHostedServices
    public int timeWaitHostedServices = 10;

}