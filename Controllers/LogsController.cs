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
        public void Post(string message, string stacktrace)
        {
            LogMessage _logMessage = new LogMessage();
            _logMessage.Details = stacktrace;
            _logMessage.Message = message;
            _appContext.Add(_logMessage);
            _appContext.SaveChanges();

        }


    }
}
