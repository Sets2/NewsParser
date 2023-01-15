using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsParser.Core.Domain;
using NewsParser.Models;
using WebApi.Mappers;

namespace NewsParser.Controllers
{
    /// <summary>
    /// Новостные каналы
    /// </summary>
    //[ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ItemController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly ILogger<ItemController> _logger;
        public ItemController(DataContext dataContext, ILogger<ItemController> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        /// <summary>
        /// Получить данные всех элементов каналов, в качестве фильтра можно указать один или несколько параметров
        /// </summary>
        /// <param name="сhannelId">ChannelId канала, например <example>1</example></param>
        /// <param name="link">ссылка на новость(элемент) канала, например <example>https://www.vedomosti.ru/newspaper/out/rss.xml</example></param>
        /// <param name="title">заголовок новости(элемента) канала, например <example>"Ведомости". Ежедневная деловая газета</example></param>
        /// <param name="description">описание новости(элемента) канала, например <example>"Ведомости". Новости, 14.01.2023</example></param>
        /// <param name="isReaded">Статус новости(элемента) канала (прочитана true/нет false), например <example>false</example></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<Item>> GetItemAsync([FromQuery] long? сhannelId, [FromQuery] string? link, 
            [FromQuery] string? title, [FromQuery] string? description, [FromQuery] bool? isReaded)
        {
            try
            {
                var query = _dataContext.Item.AsQueryable();
                if (сhannelId != null) query = query.Where(x => x.ChannelId == сhannelId);
                if (!String.IsNullOrEmpty(link)) query = query.Where(x=>x.Link!.Contains(link));
                if (!String.IsNullOrEmpty(title)) query = query.Where(x => x.Title!.Contains(title));
                if (!String.IsNullOrEmpty(description)) query = query.Where(x => x.Description!.Contains(description));
                if (isReaded !=null) query = query.Where(x => x.IsReaded==isReaded);
                var items = await query.ToListAsync();

                return Ok(items);
            }
            catch (Exception e)
            {
                var err = "Ошибка получения данных из таблицы item БД";
                _logger.LogError(e,e.Message + " " + err);
                return Problem(err);
            }
        }

        /// <summary>
        /// Обновить статус новости(элемент) канала о прочтении по id
        /// </summary>
        /// <param name="id">Id канала, например <example>1</example></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> EditItemAsync(long id, CreateEditItemRequest request)
        {
            try
            {
                var item= await _dataContext.Item.FirstOrDefaultAsync(x => x.Id == id);
                if (item == null) return NotFound();
                ItemMapper.MapFromModel(request, item);
                await _dataContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                var err = "Ошибка обновления данных в таблице item БД";
                _logger.LogError(e, e.Message + " " + err);
                return Problem(err);
            }
        }

        /// <summary>
        /// Удалить новость(элемент) канала по id
        /// </summary>
        /// <param name="id">Id новость(элемент) канала, например <example>1</example></param>
        /// <returns></returns>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteItemAsync(long id)
        {
            try
            {
                var item = await _dataContext.Item.FirstOrDefaultAsync(x => x.Id == id);
                if (item == null) return NotFound();
                _dataContext.Item.Remove(item);
                await _dataContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                var err = "Ошибка обновления данных в таблице item БД";
                _logger.LogError(e, e.Message + " " + err);
                return Problem(err);
            }
        }
    }
}
