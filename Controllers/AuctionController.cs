using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LinkshellManager.Data;
using LinkshellManager.ViewModels;
using System.Threading.Tasks;
using LinkshellManager.Models;
using System.Linq;
using NodaTime;
using NodaTime.TimeZones;

namespace LinkshellManager.Controllers
{
    public class AuctionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuctionController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IDateTimeZoneProvider _dateTimeZoneProvider;

        public AuctionController(ApplicationDbContext context, ILogger<AuctionController> logger, UserManager<AppUser> userManager, IDateTimeZoneProvider dateTimeZoneProvider)
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

                _logger.LogInformation("Fetching auctions for user's primary linkshell: {UserId}", user.Id);

                var primaryLinkshellId = user.PrimaryLinkshellId;

                // Get auctions where the user's primary linkshell is the same as the auction's linkshell
                var auctions = await _context.Auctions
                    .Include(a => a.AuctionItems)
                    .Where(a => a.LinkshellId == primaryLinkshellId)
                    .ToListAsync();

                _logger.LogInformation("Found {AuctionCount} auctions for user's primary linkshell.", auctions.Count);

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

                var viewModel = auctions.Select(auction => new AuctionViewModel
                {
                    Auction = auction,
                    AuctionItems = auction.AuctionItems.Select(ai => new AuctionItem
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
                _logger.LogError(ex, "An error occurred while fetching auctions.");
                return StatusCode(500, "Internal server error");
            }
        }

[HttpGet]
public async Task<IActionResult> CreateAsync()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null)
    {
        _logger.LogWarning("User is not authenticated.");
        return Challenge(); // Redirect to login if user is not authenticated
    }

    var userLinkshells = await _context.AppUserLinkshells
        .Where(ul => ul.AppUserId == user.Id)
        .Select(ul => ul.Linkshell)
        .ToListAsync();

    var viewModel = new AuctionViewModel
    {
        Linkshells = userLinkshells,
        LinkshellId = user.PrimaryLinkshellId ?? 0 // Assuming PrimaryLinkshellId is nullable
    };

    return View(viewModel);
}

        [HttpPost]
        public async Task<IActionResult> Create(AuctionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogError(error.ErrorMessage);
                }

                model.Linkshells = _context.Linkshells.ToList();
                return View(model);
            }

            var linkshell = await _context.Linkshells.FindAsync(model.LinkshellId);
            if (linkshell == null)
            {
                ModelState.AddModelError("LinkshellId", "Invalid Linkshell selected.");
                model.Linkshells = _context.Linkshells.ToList();
                return View(model);
            }

            // Assume the input is in local time and convert to UTC
            var startTimeUtc = model.Auction.StartTime.HasValue ? DateTime.SpecifyKind(model.Auction.StartTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;
            var endTimeUtc = model.Auction.EndTime.HasValue ? DateTime.SpecifyKind(model.Auction.EndTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;

            var auction = new Auction
            {
                AuctionTitle = model.Auction.AuctionTitle,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                LinkshellId = model.LinkshellId,
                CreatedBy = user?.CharacterName,
                AuctionItems = model.AuctionItems.Select(ai => new AuctionItem
                {
                    ItemName = ai.ItemName,
                    ItemType = ai.ItemType,
                    StartingBidDkp = ai.StartingBidDkp,
                    EndingBidDkp = ai.EndingBidDkp,
                    CurrentHighestBid = 0,
                    CurrentHighestBidder = "No bids",
                    StartTime = startTimeUtc,
                    EndTime = endTimeUtc,
                    Status = "Pending",
                    Notes = ai.Notes
                }).ToList()
            };

            _context.Add(auction);
            await _context.SaveChangesAsync();

            // Add notification
            var users = await _context.Users.Include(u => u.Notifications).ToListAsync();
            foreach (var appUser in users)
            {
                var notification = new Notification
                {
                    NotificationType = "Auction",
                    NotificationDetails = $"New auction created: {auction.AuctionTitle}",
                    CreatedAt = DateTime.UtcNow
                };
                appUser.Notifications.Add(notification);
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(AuctionItem newItem, int auctionId)
        {
            if (ModelState.IsValid)
            {
                var auction = await _context.Auctions.Include(a => a.AuctionItems)
                                                    .FirstOrDefaultAsync(a => a.Id == auctionId);
                if (auction == null)
                {
                    return NotFound();
                }

                auction.AuctionItems.Add(new AuctionItem
                {
                    ItemName = newItem.ItemName,
                    ItemType = newItem.ItemType,
                    StartingBidDkp = newItem.StartingBidDkp,
                    StartTime = DateTime.SpecifyKind(auction.StartTime.Value, DateTimeKind.Utc),
                    EndTime = DateTime.SpecifyKind(auction.EndTime.Value, DateTimeKind.Utc),
                    CurrentHighestBid = 0,
                    CurrentHighestBidder = "No bids",
                    Status = "Pending"
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetBids(int auctionItemId)
        {
            var bids = await _context.Bids
                .Where(b => b.AuctionItemId == auctionItemId)
                .Select(b => new { b.CharacterName, b.BidAmount })
                .ToListAsync();

            return Json(bids);
        }

        [HttpPost]
        public async Task<IActionResult> MakeBid(int auctionItemId, string characterName, int bidAmount)
        {
            var auctionItem = await _context.AuctionItems.Include(ai => ai.Bids).FirstOrDefaultAsync(ai => ai.Id == auctionItemId);
            if (auctionItem == null)
            {
                return NotFound();
            }

            var minBid = auctionItem.CurrentHighestBid ?? auctionItem.StartingBidDkp;
            if (bidAmount <= minBid)
            {
                ModelState.AddModelError("BidAmount", $"Bid must be higher than {minBid}");
                return RedirectToAction(nameof(Index));
            }

            var bid = new Bid
            {
                AuctionItemId = auctionItemId,
                CharacterName = characterName,
                BidAmount = bidAmount
            };

            auctionItem.Bids.Add(bid);
            auctionItem.CurrentHighestBid = bidAmount;
            auctionItem.CurrentHighestBidder = characterName;

            _context.Update(auctionItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult RemoveItem(int id)
        {
            var auctionItem = _context.AuctionItems.Find(id);
            if (auctionItem == null)
            {
                return NotFound();
            }

            _context.AuctionItems.Remove(auctionItem);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> EndAuction(int auctionId)
        {
            var auction = await _context.Auctions
                .Include(a => a.AuctionItems)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null)
            {
                return NotFound();
            }

            // Convert times to UTC
            var startTimeUtc = auction.StartTime.HasValue ? DateTime.SpecifyKind(auction.StartTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;
            var endTimeUtc = auction.EndTime.HasValue ? DateTime.SpecifyKind(auction.EndTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;

            // Create a new AuctionHistory entity
            var auctionHistory = new AuctionHistory
            {
                LinkshellId = auction.LinkshellId,
                AuctionTitle = auction.AuctionTitle,
                CreatedBy = auction.CreatedBy,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                AuctionItems = auction.AuctionItems.Select(ai => new AuctionItem
                {
                    ItemName = ai.ItemName,
                    ItemType = ai.ItemType,
                    StartingBidDkp = ai.StartingBidDkp,
                    CurrentHighestBid = ai.CurrentHighestBid,
                    CurrentHighestBidder = ai.CurrentHighestBidder,
                    EndingBidDkp = ai.CurrentHighestBid,
                    StartTime = ai.StartTime.HasValue ? DateTime.SpecifyKind(ai.StartTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null,
                    EndTime = ai.EndTime.HasValue ? DateTime.SpecifyKind(ai.EndTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null,
                    Status = ai.Status,
                    Notes = ai.Notes
                }).ToList()
            };

            // Add the AuctionHistory entity to the context
            _context.AuctionHistories.Add(auctionHistory);

            // Subtract DKP from the winning bidders
            foreach (var item in auction.AuctionItems)
            {
                if (!string.IsNullOrEmpty(item.CurrentHighestBidder) && item.CurrentHighestBid.HasValue)
                {
                    var winner = await _context.AppUserLinkshells
                        .FirstOrDefaultAsync(ul => ul.CharacterName == item.CurrentHighestBidder && ul.LinkshellId == auction.LinkshellId);

                    if (winner != null)
                    {
                        var previousDkp = winner.LinkshellDkp ?? 0;
                        winner.LinkshellDkp -= item.CurrentHighestBid.Value;
                        _context.Update(winner);

                        // Add entry to DkpLedger
                        var dkpLedger = new DkpLedger
                        {
                            AppUserLinkshellId = winner.Id,
                            DkpType = "Auction",
                            TransactionType = "Subtract",
                            TransactionValue = item.CurrentHighestBid.Value,
                            PreviousDkp = previousDkp,
                            NewDkp = winner.LinkshellDkp
                        };
                        _context.DkpLedgers.Add(dkpLedger);
                    }
                }

                item.AuctionId = null; // Remove the foreign key reference
            }

            // Remove the auction from the Auction entity
            _context.Auctions.Remove(auction);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAuctionDetails(int auctionId)
        {
            var auction = await _context.Auctions
                .Include(a => a.AuctionItems)
                .ThenInclude(ai => ai.Bids)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var timeZoneId = user?.TimeZone;
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

            var auctionDetails = auction.AuctionItems.Select(ai => new
            {
                itemName = ai.ItemName,
                highestBid = ai.CurrentHighestBid ?? ai.StartingBidDkp,
                highestBidder = ai.CurrentHighestBidder ?? "No bids",
                startTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(ai.StartTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified(),
                endTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(ai.EndTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified()
            }).ToList();

            return Json(auctionDetails);
        }
    }
}