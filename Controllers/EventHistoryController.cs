using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using LinkshellManager.Data;
using LinkshellManager.Models;
using NodaTime;
using NodaTime.TimeZones;
using Microsoft.AspNetCore.Identity;

namespace LinkshellManager.Controllers
{
    public class EventHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EventHistoryController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var timeZoneId = user?.TimeZone ?? "UTC"; // Default to UTC if no timezone is set

            var eventHistories = await _context.EventHistories.ToListAsync();

            foreach (var eventHistory in eventHistories)
            {
                eventHistory.StartTime = ConvertUtcToUserTimeZone(eventHistory.StartTime, timeZoneId);
                eventHistory.EndTime = ConvertUtcToUserTimeZone(eventHistory.EndTime, timeZoneId);
            }

            return View(eventHistories);
        }

        public DateTime? ConvertUtcToUserTimeZone(DateTime? utcDateTime, string timeZoneId)
        {
            if (!utcDateTime.HasValue)
            {
                return null;
            }

            var userTimeZone = DateTimeZoneProviders.Tzdb[timeZoneId];
            var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcDateTime.Value, DateTimeKind.Utc));
            return instant.InZone(userTimeZone).ToDateTimeUnspecified();
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var timeZoneId = user?.TimeZone ?? "UTC"; // Default to UTC if no timezone is set

            var eventHistory = await _context.EventHistories
                .Include(eh => eh.AppUserEventHistories)
                .FirstOrDefaultAsync(eh => eh.Id == id);

            if (eventHistory == null)
            {
                return NotFound();
            }

            eventHistory.StartTime = ConvertUtcToUserTimeZone(eventHistory.StartTime, timeZoneId);
            eventHistory.EndTime = ConvertUtcToUserTimeZone(eventHistory.EndTime, timeZoneId);

            return View(eventHistory);
        }
    }
}