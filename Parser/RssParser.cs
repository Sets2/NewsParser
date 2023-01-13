using System.Net.Http;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace NewsParser.Parser;

public class RssParser
{
    //private const string _uri = "http://static.feed.rbc.ru/rbc/logical/footer/news";
    private const string _uri = "https://lenta.ru/rss/google-newsstand/main/";
    //private const string _uri = "https://www.kommersant.ru/RSS/news.xml";
    //private const string _uri = "http://www.vedomosti.ru/newspaper/out/rss.xml";
    //private const string _uri = "http:https://www.vedomosti.ru/rss/articles";
    //private const string _uri = "https://eadaily.com/ru/rss/index.xml";

    private HttpClient _httpClient;
    private XmlDocument _xDoc;

    public RssParser()
    {
        _httpClient = new HttpClient();
        _xDoc = new XmlDocument();
    }

    public async Task GetData()
    {
        //HttpResponseMessage response;
        string content;

        using (var request = new HttpRequestMessage(HttpMethod.Get, _uri))
        {
            //request.Headers.Add("Accept", "text/xml,text/html,application/xhtml+xml,application/xml");
            request.Headers.Add("Accept-Charset", "UTF-8");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; rv:77.0) Gecko/20100101 Firefox/77.0");
            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead))
            {
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
        }
        Console.WriteLine(content);

        _xDoc.LoadXml(content);

    }
}