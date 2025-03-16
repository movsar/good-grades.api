using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GGLogsApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationContext _appContext;

        public LogsController(ApplicationContext appContext)
        {
            _appContext = appContext;
        }

        [HttpPost]
        public async Task Post(List<LogMessage> messages)
        {
            await _appContext.AddRangeAsync(messages);
            await _appContext.SaveChangesAsync();
        }

        [HttpGet]
        public List<LogMessage> Get(int count)
        {
            List<LogMessage> result = _appContext.LogMessages
                .AsEnumerable()
                .OrderBy(item => item.CreatedAt)
                .Take(count)
                .ToList();

            return result;
        }
    }
}
