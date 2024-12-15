using LinkshellManager.Data;
using LinkshellManager.ViewModels;
using LinkshellManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LinkshellManager.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<AppUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? weekInput = null, int? linkshellId = null)
        {
            Console.WriteLine("LinkshellId: " + linkshellId);
            DateTime currentDateTime = DateTime.Now;
            DateTime weekOf = weekInput ?? StartOfWeek(currentDateTime, DayOfWeek.Sunday);

            var user = await _userManager.GetUserAsync(User);
            Console.WriteLine($"User: {user.PrimaryLinkshellName}");
            Console.WriteLine($"User: {user.PrimaryLinkshellId}");
            AppUserLinkshell appUserLinkshell = null;

            // Fetch linkshells that the user is a member of
            var userLinkshells = await _context.AppUserLinkshells
                .Where(aul => aul.AppUserId == user.Id)
                .Select(aul => aul.Linkshell)
                .ToListAsync();

            if (linkshellId.HasValue)
            {
                appUserLinkshell = await _context.AppUserLinkshells
                    .FirstOrDefaultAsync(aul => aul.AppUserId == user.Id && aul.LinkshellId == linkshellId.Value);
            }
            else if (!string.IsNullOrEmpty(user.PrimaryLinkshellName))
            {
                var primaryLinkshell = userLinkshells
                    .FirstOrDefault(l => l.LinkshellName == user.PrimaryLinkshellName);
                if (primaryLinkshell != null)
                {
                    linkshellId = primaryLinkshell.Id;
                    appUserLinkshell = await _context.AppUserLinkshells
                        .FirstOrDefaultAsync(aul => aul.AppUserId == user.Id && aul.LinkshellId == primaryLinkshell.Id);
                }
            }
            else
            {
                appUserLinkshell = await _context.AppUserLinkshells
                    .FirstOrDefaultAsync(aul => aul.AppUserId == user.Id);
            }

            var model = new DashboardViewModel
            {
                Linkshells = userLinkshells,
                RecentSales = await FetchRecentSalesAsync(user.PrimaryLinkshellId),
                Tods = await FetchTodsAsync(weekOf, user.PrimaryLinkshellId),
                Events = await FetchEventsAsync(weekOf, user.PrimaryLinkshellId),
                EventHistories = await FetchEventHistoriesAsync(weekOf, user.PrimaryLinkshellId),
                HnmClaimsPercentages = CalculateHnmClaimsPercentages(await FetchTodsAsync(weekOf, user.PrimaryLinkshellId)),
                RecentAnnouncements = await FetchRecentAnnouncementsAsync(user.PrimaryLinkshellId),
                SelectedLinkshellId = user.PrimaryLinkshellId
            };

            if (linkshellId.HasValue)
            {
                await PopulateSelectedLinkshellDetailsAsync(model, linkshellId.Value);
            }

            CalculateMonthlySums(model);

            var months = Enumerable.Range(1, 12).Select(m => new DateTime(2023, m, 1).ToString("MMM")).ToList();
            var scaledRevenueData = model.RevenueData.Select(r => r / 1000000.0).ToArray();

            ViewBag.Months = months;
            ViewBag.MembersData = model.MembersData;
            ViewBag.ItemsData = model.ItemsData;
            ViewBag.RevenueData = scaledRevenueData;

            ViewBag.ImageFiles = FetchImageFiles();

            ViewData["CharacterName"] = appUserLinkshell?.CharacterName;
            ViewData["Rank"] = appUserLinkshell?.Rank;

            return View(model);
        }

        private async Task<List<Linkshell>> FetchLinkshellsAsync()
        {
            return await _context.Linkshells.ToListAsync();
        }

private async Task<List<AuctionHistory>> FetchRecentSalesAsync(int? linkshellId)
{
    return await _context.AuctionHistories
        .Include(ah => ah.AuctionItems)
        .Where(ah => ah.LinkshellId == linkshellId)
        .OrderByDescending(ah => ah.EndTime)
        .Take(5)
        .ToListAsync();
}

private async Task<List<Tod>> FetchTodsAsync(DateTime weekOf, int? linkshellId)
{
    return await _context.Tods
        .Include(t => t.TodLootDetails)
        .Where(t => t.TimeStamp >= weekOf && t.LinkshellId == linkshellId)
        .OrderByDescending(t => t.TimeStamp)
        .ToListAsync();
}

private async Task<List<Event>> FetchEventsAsync(DateTime weekOf, int? linkshellId)
{
    return await _context.Events
        .Include(e => e.EventLootDetails)
        .Where(e => e.TimeStamp >= weekOf && e.LinkshellId == linkshellId)
        .OrderByDescending(e => e.TimeStamp)
        .ToListAsync();
}

private async Task<List<EventHistory>> FetchEventHistoriesAsync(DateTime weekOf, int? linkshellId)
{
    return await _context.EventHistories
        .Where(eh => eh.TimeStamp >= weekOf && eh.LinkshellId == linkshellId)
        .OrderByDescending(eh => eh.TimeStamp)
        .ToListAsync();
}

private Dictionary<string, double> CalculateHnmClaimsPercentages(List<Tod> tods)
{
    var hnmNames = new[] { "Adamantoise", "Behemoth", "Fafnir", "Aspidochelone", "King Behemoth", "Nidhogg", "Tiamat", "Jormungand", "Vrtra" };
    var hnmClaimsPercentages = new Dictionary<string, double>();

    foreach (var hnm in hnmNames)
    {
        var totalTods = tods.Where(t => t.MonsterName == hnm).Sum(t => t.TotalTods ?? 0);
        var totalClaims = tods.Where(t => t.MonsterName == hnm).Sum(t => t.TotalClaims ?? 0);

        var percentage = totalTods > 0 ? (double)totalClaims / totalTods * 100 : 0;
        hnmClaimsPercentages[hnm] = percentage;
    }

    return hnmClaimsPercentages;
}

private async Task<List<Announcement>> FetchRecentAnnouncementsAsync(int? linkshellId)
{
    return await _context.Announcements
        .Where(a => a.LinkshellId == linkshellId)
        .OrderByDescending(a => a.Id)
        .Take(3)
        .ToListAsync();
}

        private void CalculateMonthlySums(DashboardViewModel model)
        {
            var membersByMonth = _context.AppUserLinkshells
                .Where(aul => aul.DateJoined.HasValue)
                .GroupBy(aul => new { aul.DateJoined.Value.Year, aul.DateJoined.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToList();

            var itemsByMonth = _context.Items
                .Where(i => i.TimeStamp.HasValue)
                .GroupBy(i => new { i.TimeStamp.Value.Year, i.TimeStamp.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(i => i.Quantity) })
                .ToList();

            var revenueByMonth = _context.Incomes
                .Where(i => i.TimeStamp.HasValue)
                .GroupBy(i => new { i.TimeStamp.Value.Year, i.TimeStamp.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(i => i.Value) })
                .ToList();

            model.MembersData = new int[12];
            model.ItemsData = new int[12];
            model.RevenueData = new int[12];

            foreach (var member in membersByMonth)
            {
                model.MembersData[member.Month - 1] = member.Count;
            }

            foreach (var item in itemsByMonth)
            {
                model.ItemsData[item.Month - 1] = (int)item.Sum;
            }

            foreach (var revenue in revenueByMonth)
            {
                model.RevenueData[revenue.Month - 1] = (int)revenue.Sum;
            }
        }

        private HashSet<string> FetchImageFiles()
        {
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ffxi_assets");
            return Directory.GetFiles(imagePath, "*.*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), path))
                .ToHashSet();
        }

        private async Task PopulateSelectedLinkshellDetailsAsync(DashboardViewModel model, int linkshellId)
        {
            var selectedLinkshell = await _context.Linkshells
                .Include(ls => ls.AppUserLinkshells)
                .ThenInclude(uls => uls.AppUser)
                .Include(ls => ls.Items)
                .Include(ls => ls.Incomes)
                .FirstOrDefaultAsync(ls => ls.Id == linkshellId);

            if (selectedLinkshell != null)
            {
                model.TotalMembers = selectedLinkshell.AppUserLinkshells?.Count ?? 0;
                model.TotalItems = selectedLinkshell.Items?.Count ?? 0;
                model.Revenue = selectedLinkshell.Incomes?.Sum(i => i.Value) ?? 0;
                model.SelectedLinkshellId = selectedLinkshell.Id;
                model.Members = selectedLinkshell.AppUserLinkshells?.ToList();
            }
        }

        private DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }


    }
}