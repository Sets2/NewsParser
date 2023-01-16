using NewsParser.Core.Domain;
using NewsParser.Models;

namespace WebApi.Mappers
{
    public static class ItemMapper
    {
        public static Item MapFromModel(CreateEditItemRequest request, Item? item = null)
        {
            if (item == null) item = new Item();
            item.IsReaded = request.IsReaded;

            return item;
        }
    }
}