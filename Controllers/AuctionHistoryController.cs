using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinkshellManager.Data;
using LinkshellManager.ViewModels;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using LinkshellManager.Models;
using NodaTime;
using NodaTime.TimeZones;

namespace LinkshellManager.Controllers
{
    public class AuctionHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuctionHistoryController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IDateTimeZoneProvider _dateTimeZoneProvider;

        public AuctionHistoryController(ApplicationDbContext context, ILogger<AuctionHistoryController> logger, UserManager<AppUser> userManager, IDateTimeZoneProvider dateTimeZoneProvider)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _dateTimeZoneProvider = dateTimeZoneProvider;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User is not authenticated.");
                    return Challenge(); // Redirect to login if user is not authenticated
                }

                _logger.LogInformation("Fetching auction history for user's primary linkshell: {UserId}", user.Id);

                var primaryLinkshellId = user.PrimaryLinkshellId;

                // Get auction history where the user's primary linkshell is the same as the auction's linkshell
                var auctionHistories = await _context.AuctionHistories
                    .Include(ah => ah.AuctionItems)
                    .Where(ah => ah.LinkshellId == primaryLinkshellId)
                    .ToListAsync();

                _logger.LogInformation("Found {AuctionHistoryCount} auction histories for user's primary linkshell.", auctionHistories.Count);

                var timeZoneId = user.TimeZone;
                DateTimeZone userTimeZone;
                if (!string.IsNullOrEmpty(timeZoneId))
                {
                    try
                    {
                        userTimeZone = _dateTimeZoneProvider[timeZoneId];
                    }
                    catch (DateTimeZoneNotFoundException)
                    {
                        _logger.LogWarning("Time zone ID '{TimeZoneId}' not found. Using UTC.", timeZoneId);
                        userTimeZone = DateTimeZone.Utc;
                    }
                }
                else
                {
                    _logger.LogWarning("Time zone ID is null. Using UTC.");
                    userTimeZone = DateTimeZone.Utc;
                }

                var viewModel = auctionHistories.Select(auctionHistory => new AuctionHistoryViewModel
                {
                    AuctionHistory = new AuctionHistory
                    {
                        AuctionTitle = auctionHistory.AuctionTitle,
                        StartTime = auctionHistory.StartTime.HasValue ? Instant.FromDateTimeUtc(DateTime.SpecifyKind(auctionHistory.StartTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified() : (DateTime?)null,
                        EndTime = auctionHistory.EndTime.HasValue ? Instant.FromDateTimeUtc(DateTime.SpecifyKind(auctionHistory.EndTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified() : (DateTime?)null
                    },
                    AuctionItems = auctionHistory.AuctionItems.Select(ai => new AuctionItem
                    {
                        Id = ai.Id,
                        ItemName = ai.ItemName,
                        ItemType = ai.ItemType,
                        StartingBidDkp = ai.StartingBidDkp,
                        EndingBidDkp = ai.EndingBidDkp,
                        CurrentHighestBid = ai.CurrentHighestBid,
                        CurrentHighestBidder = ai.CurrentHighestBidder,
                        StartTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(ai.StartTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified(),
                        EndTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(ai.EndTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified(),
                        Status = ai.Status,
                        Notes = ai.Notes
                    }).ToList()
                }).ToList();

                ViewBag.CharacterName = user.CharacterName;
                ViewBag.CurrentUserId = user.Id;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching auction histories.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItemStatus(int itemId)
        {
            var auctionItem = await _context.AuctionItems.FindAsync(itemId);
            if (auctionItem == null)
            {
                return NotFound();
            }

            auctionItem.Status = "Received";
            _context.Update(auctionItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UndoItemStatus(int itemId)
        {
            var auctionItem = await _context.AuctionItems.FindAsync(itemId);
            if (auctionItem == null)
            {
                return NotFound();
            }

            auctionItem.Status = "Pending";
            _context.Update(auctionItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}