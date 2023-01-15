using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewsParser.Parser;

namespace Otus.Teaching.Pcf.Administration.Integration
{
    public class ParserHostedService : BackgroundService
    {
        private readonly IGroupChannels _groupChannels;
        private readonly int _timeWait;

        public ParserHostedService(IGroupChannels groupChannels, IOptions<ParserSettings> options)
        {
            _groupChannels = groupChannels;
            _timeWait = options.Value.timeWaitHostedServices * 1000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            while (true)
            {
                await _groupChannels.UpdateChannels();
                await _groupChannels.ReadChannels();
                await Task.Delay(_timeWait);
            }
        }
    }
}
