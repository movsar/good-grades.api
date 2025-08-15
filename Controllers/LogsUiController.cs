using GGLogsApi.Models;
using GGLogsApi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GGLogsApi.Controllers
{
    public class LogsUiController : Controller
    {
        private readonly ApplicationContext _db;

        public LogsUiController(ApplicationContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] LogsFilter filter)
        {
            // --- Defaults & guards for date range (UTC) ---
            // If both are missing → last 24h
            var nowUtc = DateTime.UtcNow;
            if (!filter.From.HasValue && !filter.To.HasValue)
            {
                filter.To = nowUtc;
                filter.From = nowUtc.AddDays(-1);
            }
            // If only From is provided → assume 24h window forward
            else if (filter.From.HasValue && !filter.To.HasValue)
            {
                filter.To = filter.From.Value.AddDays(1);
            }
            // If only To is provided → assume 24h window backward
            else if (!filter.From.HasValue && filter.To.HasValue)
            {
                filter.From = filter.To.Value.AddDays(-1);
            }
            // Swap if user passed reversed bounds
            if (filter.From.HasValue && filter.To.HasValue && filter.From > filter.To)
            {
                (filter.From, filter.To) = (filter.To, filter.From);
            }

            // --- Pagination guards ---
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1) filter.PageSize = 100;
            if (filter.PageSize > 200) filter.PageSize = 200;

            // --- Query ---
            IQueryable<LogMessage> q = _db.LogMessages.AsNoTracking();

            if (filter.From.HasValue) q = q.Where(x => x.CreatedAt >= filter.From.Value);
            if (filter.To.HasValue) q = q.Where(x => x.CreatedAt <= filter.To.Value);

            if (filter.Level.HasValue) q = q.Where(x => x.Level >= filter.Level.Value);

            if (!string.IsNullOrWhiteSpace(filter.ProgramName)) q = q.Where(x => x.ProgramName.Contains(filter.ProgramName));
            if (!string.IsNullOrWhiteSpace(filter.ProgramVersion)) q = q.Where(x => x.ProgramVersion.Contains(filter.ProgramVersion));
            if (!string.IsNullOrWhiteSpace(filter.WindowsVersion)) q = q.Where(x => x.WindowsVersion.Contains(filter.WindowsVersion));
            if (!string.IsNullOrWhiteSpace(filter.SystemDetails)) q = q.Where(x => x.SystemDetails == filter.SystemDetails);
            if (filter.HasStackTrace.HasValue)
            {
                if (filter.HasStackTrace.Value) q = q.Where(x => x.StackTrace != null && x.StackTrace != "");
                else q = q.Where(x => x.StackTrace == null || x.StackTrace == "");
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                q = q.Where(x => x.Message.Contains(filter.Search) ||
                                 (x.StackTrace != null && x.StackTrace.Contains(filter.Search)));
            }

            // Sorting
            bool desc = string.Equals(filter.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
            q = (filter.SortBy?.ToLowerInvariant()) switch
            {
                "level" => desc ? q.OrderByDescending(x => x.Level)
                                : q.OrderBy(x => x.Level),
                "programname" => desc ? q.OrderByDescending(x => x.ProgramName)
                                      : q.OrderBy(x => x.ProgramName),
                _ => desc ? q.OrderByDescending(x => x.CreatedAt)
                          : q.OrderBy(x => x.CreatedAt),
            };

            var total = await q.CountAsync();
            var items = await q.Skip((filter.Page - 1) * filter.PageSize)
                               .Take(filter.PageSize)
                               .ToListAsync();

            var vm = new PagedLogsViewModel
            {
                Items = items,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Filter = filter // contains the normalized From/To, so your view inputs will be prefilled
            };

            // For SystemDetails dropdown
            ViewBag.Systems = await _db.LogMessages.AsNoTracking()
                .Where(x => x.SystemDetails != null && x.SystemDetails != "")
                .Select(x => x.SystemDetails!)
                .Distinct()
                .OrderBy(x => x)
                .Take(2000)
                .ToListAsync();

            return View("Index", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var item = await _db.LogMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            return View("Details", item);
        }

        [HttpGet]
        public async Task<IActionResult> Systems([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? programName = null)
        {
            IQueryable<LogMessage> q = _db.LogMessages.AsNoTracking();
            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(programName)) q = q.Where(x => x.ProgramName.Contains(programName));

            var rows = await q
                .Where(x => x.SystemDetails != null && x.SystemDetails != "")
                .GroupBy(x => x.SystemDetails)
                .Select(g => new SystemsViewModel.SystemRow
                {
                    SystemDetails = g.Key!,
                    Count = g.Count(),
                    LastLogAt = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(x => x.LastLogAt)
                .ToListAsync();

            return View("Systems", new SystemsViewModel { Items = rows });
        }

        [HttpGet]
        public async Task<IActionResult> Stats([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var nowUtc = DateTime.UtcNow;

            // --- Normalize date range ---
            if (!from.HasValue && !to.HasValue)
            {
                // Default: last 24h
                from = nowUtc.AddDays(-1);
                to = nowUtc;
            }
            else if (from.HasValue && !to.HasValue)
            {
                // Only from provided -> 24h window forward
                to = from.Value.AddDays(1);
            }
            else if (!from.HasValue && to.HasValue)
            {
                // Only to provided -> 24h window backward
                from = to.Value.AddDays(-1);
            }

            // Swap if reversed (user error)
            if (from.HasValue && to.HasValue && from > to)
                (from, to) = (to, from);

            var vm = new StatsViewModel { From = from, To = to };

            // If user explicitly provided a (possibly normalized) range, show that.
            // Otherwise, also show the quick cards (last day/week/month).
            // We detect "explicit" by checking the original query string — but to keep it simple:
            // - If the caller supplied either from/to (even one), show Period
            // - If neither supplied (we defaulted both), also show the summary cards.
            bool userSuppliedAny = Request.Query.ContainsKey("from") || Request.Query.ContainsKey("to");

            if (userSuppliedAny)
            {
                IQueryable<LogMessage> q = _db.LogMessages.AsNoTracking();
                q = q.Where(x => x.CreatedAt >= from!.Value && x.CreatedAt <= to!.Value);
                vm.Period = await AggregateByLevel(q);
            }
            else
            {
                // Show the standard cards PLUS pre-filled date inputs (vm.From/To already set to last 24h)
                var lastDayFrom = nowUtc.AddDays(-1);
                var lastWeekFrom = nowUtc.AddDays(-7);
                var lastMonthFrom = nowUtc.AddDays(-30);

                vm.LastDay = await AggregateByLevel(_db.LogMessages.AsNoTracking().Where(x => x.CreatedAt >= lastDayFrom && x.CreatedAt <= nowUtc));
                vm.LastWeek = await AggregateByLevel(_db.LogMessages.AsNoTracking().Where(x => x.CreatedAt >= lastWeekFrom && x.CreatedAt <= nowUtc));
                vm.LastMonth = await AggregateByLevel(_db.LogMessages.AsNoTracking().Where(x => x.CreatedAt >= lastMonthFrom && x.CreatedAt <= nowUtc));
            }

            return View("Stats", vm);
        }

        private static async Task<Dictionary<string, int>> AggregateByLevel(IQueryable<LogMessage> q)
        {
            var arr = await q.GroupBy(x => x.Level)
                .Select(g => new { level = g.Key, count = g.Count() })
                .ToListAsync();

            var map = new Dictionary<int, string>
            {
                {0, "Trace"},
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
    }
}
