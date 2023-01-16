using Castle.Core.Internal;
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
    public class ChannelController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly ILogger<ChannelController> _logger;
        public ChannelController(DataContext dataContext, ILogger<ChannelController> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        /// <summary>
        /// Получить данные всех каналов, в качестве фильтра можно указать один или несколько параметров
        /// </summary>
        /// <param name="id">Id канала, например <example>1</example></param>
        /// <param name="link">ссылка на канал, например <example>https://www.vedomosti.ru/newspaper/out/rss.xml</example></param>
        /// <param name="title">заголовок канала, например <example>"Ведомости". Ежедневная деловая газета</example></param>
        /// <param name="description">описание канала, например <example>"Ведомости". Новости, 14.01.2023</example></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<Channel>> GetChannelAsync([FromQuery] long? id, [FromQuery] string? link, 
            [FromQuery] string? title, [FromQuery] string? description)
        {
            try
            {
                var query = _dataContext.Channel.AsQueryable();
                if (id != null) query = query.Where(x => x.Id == id);
                if (!String.IsNullOrEmpty(link)) query = query.Where(x=>x.Link.Contains(link));
                if (!String.IsNullOrEmpty(title)) query = query.Where(x => x.Title!.Contains(title));
                if (!String.IsNullOrEmpty(description)) query = query.Where(x => x.Description!.Contains(description));
                var channels = await query.ToListAsync();
                var str = query.ToQueryString();
                return Ok(channels);
            }
            catch (Exception e)
            {
                var err = "Ошибка получения данных из таблицы channel БД";
                _logger.LogError(e,e.Message + " " + err);
                return Problem(err);
            }
        }

        /// <summary>
        /// Создать канал
        /// </summary>
        /// <param name="request"></param>
        [HttpPost]
        public async Task<ActionResult> CreateChannelAsync(CreateEditChannelRequest request)
        {
            if (request.Link == null)
                return BadRequest("Должна быть указана ссылка link");
            var channel = ChannelMapper.MapFromModel(request);
            try
            {
                await _dataContext.Channel.AddAsync(channel);
                await _dataContext.SaveChangesAsync();
                return Ok(channel);
            }
            catch (Exception e)
            {
                var err = "Ошибка получения данных из таблицы channel БД";
                _logger.LogError(e, e.Message + " " + err);
                return Problem(err);
            }
        }

        /// <summary>
        /// Обновить канал по id
        /// </summary>
        /// <param name="id">Id канала, например <example>1</example></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> EditChannelAsync(long id, CreateEditChannelRequest request)
        {
            if (request.Link == null)
                return BadRequest("Должна быть указана ссылка link");
            try
            {
                var channel= await _dataContext.Channel.FirstOrDefaultAsync(x => x.Id == id);
                if (channel == null) return NotFound();
                ChannelMapper.MapFromModel(request, channel);
                await _dataContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                var err = "Ошибка обновления данных в таблице channel БД";
                _logger.LogError(e, e.Message + " " + err);
                return Problem(err);
            }
        }

        /// <summary>
        /// Удалить канал по id
        /// </summary>
        /// <param name="id">Id канала, например <example>1</example></param>
        /// <returns></returns>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteChannelAsync(long id)
        {
            try
            {
                var channel = await _dataContext.Channel.FirstOrDefaultAsync(x => x.Id == id);
                if (channel == null) return NotFound();
                _dataContext.Channel.Remove(channel);
                await _dataContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                var err = "Ошибка обновления данных в таблице channel БД";
                _logger.LogError(e, e.Message + " " + err);
                return Problem(err);
            }
        }
    }
}
