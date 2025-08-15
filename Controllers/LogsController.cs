using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GGLogsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationContext _db;

        public LogsController(ApplicationContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int? level = null,
            [FromQuery] string? programName = null,
            [FromQuery] string? programVersion = null,
            [FromQuery] string? windowsVersion = null,
            [FromQuery] string? systemDetails = null,
            [FromQuery] bool? hasStackTrace = null,
            [FromQuery] string? search = null,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortDir = "desc")
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;
            if (pageSize > 200) pageSize = 200;

            IQueryable<LogMessage> q = _db.LogMessages.AsNoTracking();

            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
            if (level.HasValue) q = q.Where(x => x.Level == level.Value);
            if (!string.IsNullOrWhiteSpace(programName)) q = q.Where(x => x.ProgramName.Contains(programName));
            if (!string.IsNullOrWhiteSpace(programVersion)) q = q.Where(x => x.ProgramVersion.Contains(programVersion));
            if (!string.IsNullOrWhiteSpace(windowsVersion)) q = q.Where(x => x.WindowsVersion.Contains(windowsVersion));
            if (!string.IsNullOrWhiteSpace(systemDetails)) q = q.Where(x => x.SystemDetails == systemDetails);
            if (hasStackTrace.HasValue)
            {
                if (hasStackTrace.Value) q = q.Where(x => x.StackTrace != null && x.StackTrace != "");
                else q = q.Where(x => x.StackTrace == null || x.StackTrace == "");
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(x => x.Message.Contains(search) || (x.StackTrace != null && x.StackTrace.Contains(search)));
            }

            // Sorting
            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            q = (sortBy?.ToLowerInvariant()) switch
            {
                "level" => desc ? q.OrderByDescending(x => x.Level) : q.OrderBy(x => x.Level),
                "programname" => desc ? q.OrderByDescending(x => x.ProgramName) : q.OrderBy(x => x.ProgramName),
                _ => desc ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new
            {
                items,
                total,
                page,
                pageSize
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = await _db.LogMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("distinct-systems")]
        public async Task<IActionResult> GetDistinctSystems([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? programName = null)
        {
            IQueryable<LogMessage> q = _db.LogMessages.AsNoTracking();
            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(programName)) q = q.Where(x => x.ProgramName.Contains(programName));

            var data = await q
                .Where(x => x.SystemDetails != null && x.SystemDetails != "")
                .GroupBy(x => x.SystemDetails)
                .Select(g => new
                {
                    systemDetails = g.Key!,
                    count = g.Count(),
                    lastLogAt = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(x => x.lastLogAt)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            // If no custom period — return lastDay, lastWeek, lastMonth buckets
            if (!from.HasValue && !to.HasValue)
            {
                var now = DateTime.UtcNow;
                var lastDayFrom = now.AddDays(-1);
                var lastWeekFrom = now.AddDays(-7);
                var lastMonthFrom = now.AddDays(-30);

                var lastDay = await AggregateByLevel(_db.LogMessages.AsNoTracking().Where(x => x.CreatedAt >= lastDayFrom && x.CreatedAt <= now));
                var lastWeek = await AggregateByLevel(_db.LogMessages.AsNoTracking().Where(x => x.CreatedAt >= lastWeekFrom && x.CreatedAt <= now));
                var lastMonth = await AggregateByLevel(_db.LogMessages.AsNoTracking().Where(x => x.CreatedAt >= lastMonthFrom && x.CreatedAt <= now));

                return Ok(new { lastDay, lastWeek, lastMonth });
            }
            else
            {
                IQueryable<LogMessage> q = _db.LogMessages.AsNoTracking();
                if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
                if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);

                var custom = await AggregateByLevel(q);
                return Ok(new { period = custom });
            }
        }

        private static async Task<object> AggregateByLevel(IQueryable<LogMessage> q)
        {
            var arr = await q
                .GroupBy(x => x.Level)
                .Select(g => new { level = g.Key, count = g.Count() })
                .ToListAsync();

            // Map to Serilog-like names (you map LogEvent -> MS LogLevel int in your sink)
            var map = new Dictionary<int, string>
            {
                {0, "Trace"},      // MS LogLevel.Trace
                {1, "Debug"},
                {2, "Information"},
                {3, "Warning"},
                {4, "Error"},
                {5, "Critical"}
            };

            var result = new Dictionary<string, int>
            {
                ["Trace"] = 0,
                ["Debug"] = 0,
                ["Information"] = 0,
                ["Warning"] = 0,
                ["Error"] = 0,
                ["Critical"] = 0
            };

            foreach (var x in arr)
            {
                if (map.TryGetValue(x.level, out var name))
                    result[name] = x.count;
            }

            result["Total"] = result.Values.Sum();
            return result;
        }

        [HttpPost]
        public async Task Post(List<LogMessage> messages)
        {
            await _db.AddRangeAsync(messages);
            await _db.SaveChangesAsync();
        }
    }
}
