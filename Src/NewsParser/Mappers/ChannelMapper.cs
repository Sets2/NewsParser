using NewsParser.Core.Domain;
using NewsParser.Models;

namespace WebApi.Mappers
{
    public static class ChannelMapper
    {
        public static Channel MapFromModel(CreateEditChannelRequest request, Channel? channel = null)
        {
            if (channel == null) channel = new Channel();
            channel.Link = request.Link;
            channel.Title = request.Title;
            channel.Description = request.Description;  

            return channel;
        }
    }
}