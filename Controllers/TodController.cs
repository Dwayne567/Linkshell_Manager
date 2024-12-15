using Microsoft.AspNetCore.Mvc;
using LinkshellManager.ViewModels;
using LinkshellManager.Models;
using LinkshellManager.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NodaTime;
using NodaTime.TimeZones;

namespace LinkshellManager.Controllers
{
    public class TodController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IDateTimeZoneProvider _dateTimeZoneProvider;

        public TodController(ApplicationDbContext context, UserManager<AppUser> userManager, IDateTimeZoneProvider dateTimeZoneProvider)
        {
            _context = context;
            _userManager = userManager;
            _dateTimeZoneProvider = dateTimeZoneProvider;
        }

public IActionResult Index()
{
    var user = _userManager.GetUserAsync(User).Result;
    var userTimeZoneId = user?.TimeZone ?? "UTC"; // Default to UTC if no time zone is set
    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
    var primaryLinkshellId = user?.PrimaryLinkshellId;

    var tods = _context.Tods
        .Include(t => t.Linkshell)
        .Where(t => t.LinkshellId == primaryLinkshellId)
        .GroupBy(t => t.MonsterName)
        .Select(g => g.OrderByDescending(t => t.Time).FirstOrDefault())
        .ToList();

    if (tods == null)
    {
        return NotFound("Tods not found.");
    }

    // Convert times to user's time zone
    foreach (var tod in tods)
    {
        if (tod.Time.HasValue)
        {
            tod.Time = TimeZoneInfo.ConvertTimeFromUtc(tod.Time.Value, userTimeZone);
        }
        if (tod.RepopTime.HasValue)
        {
            tod.RepopTime = TimeZoneInfo.ConvertTimeFromUtc(tod.RepopTime.Value, userTimeZone);
        }
    }

    var model = new TodManagerViewModel
    {
        TodItems = tods,
        Linkshells = _context.Linkshells.ToList()
    };
    return View(model);
}

[HttpGet]
public async Task<IActionResult> Create()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null)
    {
        // _logger.LogWarning("User is not authenticated.");
        return Challenge(); // Redirect to login if user is not authenticated
    }

    var userLinkshells = await _context.AppUserLinkshells
        .Where(ul => ul.AppUserId == user.Id)
        .Select(ul => ul.Linkshell)
        .ToListAsync();

    var viewModel = new TodManagerViewModel
    {
        Linkshells = userLinkshells,
        LinkshellId = user.PrimaryLinkshellId ?? 0 // Assuming PrimaryLinkshellId is nullable
    };

    return View(viewModel);
}

        [HttpPost]
        public IActionResult Create(TodManagerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newTod = new Tod
                {
                    MonsterName = model.Tod.MonsterName,
                    DayNumber = model.Tod.DayNumber,
                    Claim = model.Tod.Claim,
                    Time = model.Tod.Time.HasValue ? DateTime.SpecifyKind(model.Tod.Time.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null,
                    Cooldown = model.Tod.Cooldown,
                    RepopTime = model.Tod.RepopTime.HasValue ? DateTime.SpecifyKind(model.Tod.RepopTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null,
                    Interval = model.Tod.Interval,
                    LinkshellId = model.Tod.LinkshellId,
                    TimeStamp = DateTime.UtcNow,
                    TotalTods = 1,
                    TotalClaims = model.Tod.Claim ? 1 : 0
                };
                _context.Tods.Add(newTod);
                _context.SaveChanges();

                if (!model.NoLoot)
                {
                    foreach (var lootDetail in model.TodLootDetails)
                    {
                        lootDetail.TodId = newTod.Id;
                        _context.TodLootDetails.Add(lootDetail);

                        // Subtract DKP from the item winner
                        if (!string.IsNullOrEmpty(lootDetail.ItemWinner))
                        {
                            var winner = _context.AppUserLinkshells
                                .FirstOrDefault(ul => ul.CharacterName == lootDetail.ItemWinner);

                            if (winner != null)
                            {
                                var previousDkp = winner.LinkshellDkp ?? 0;
                                winner.LinkshellDkp -= lootDetail.WinningDkpSpent;
                                _context.Update(winner);

                                // Add entry to DkpLedger
                                var dkpLedger = new DkpLedger
                                {
                                    AppUserLinkshellId = winner.Id,
                                    DkpType = "Loot",
                                    TransactionType = "Subtract",
                                    TransactionValue = lootDetail.WinningDkpSpent,
                                    PreviousDkp = previousDkp,
                                    NewDkp = winner.LinkshellDkp
                                };
                                _context.DkpLedgers.Add(dkpLedger);
                            }
                        }
                    }
                    _context.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            model.Linkshells = _context.Linkshells.ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult GetTod(int id)
        {
            var tod = _context.Tods.Find(id);
            if (tod == null)
            {
                return NotFound();
            }

            var user = _userManager.GetUserAsync(User).Result;
            var userTimeZoneId = user?.TimeZone ?? "UTC"; // Default to UTC if no time zone is set
            var userTimeZone = _dateTimeZoneProvider[userTimeZoneId];

            Console.WriteLine($"User Time Zone: {userTimeZoneId}");

            if (tod.Time.HasValue)
            {
                var utcTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(tod.Time.Value, DateTimeKind.Utc));
                var localTime = utcTime.InZone(userTimeZone).ToDateTimeUnspecified();
                Console.WriteLine($"Original Time: {tod.Time.Value}, UTC Time: {utcTime}, Local Time: {localTime}");
                tod.Time = localTime;
            }
            if (tod.RepopTime.HasValue)
            {
                var utcRepopTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(tod.RepopTime.Value, DateTimeKind.Utc));
                var localRepopTime = utcRepopTime.InZone(userTimeZone).ToDateTimeUnspecified();
                Console.WriteLine($"Original Repop Time: {tod.RepopTime.Value}, UTC Repop Time: {utcRepopTime}, Local Repop Time: {localRepopTime}");
                tod.RepopTime = localRepopTime;
            }

            // Log the final TOD object before returning
            Console.WriteLine($"Final TOD Object: {Newtonsoft.Json.JsonConvert.SerializeObject(tod)}");

            return Json(tod);
        }

        [HttpGet]
        public IActionResult GetLootDetails(int id)
        {
            var lootDetails = _context.TodLootDetails.Where(ld => ld.TodId == id).ToList();
            if (lootDetails == null || !lootDetails.Any())
            {
                return Json(new List<TodLootDetail>());
            }
            return Json(lootDetails);
        }

        [HttpGet]
        public IActionResult GetCharacterNames()
        {
            var user = _userManager.GetUserAsync(User).Result;
            var primaryLinkshellId = user?.PrimaryLinkshellId;

            var characterNames = _context.AppUserLinkshells
                .Where(ul => ul.LinkshellId == primaryLinkshellId)
                .Select(ul => ul.CharacterName)
                .ToList();

            return Json(characterNames);
        }

        [HttpPost]
        public IActionResult Edit(TodManagerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var tod = _context.Tods.Include(t => t.TodLootDetails).FirstOrDefault(t => t.Id == model.Tod.Id);
                if (tod == null)
                {
                    return NotFound();
                }

                // Check if Claim has been changed to "No"
                if (tod.Claim && !model.Tod.Claim)
                {
                    // Remove associated TodLootDetails
                    _context.TodLootDetails.RemoveRange(tod.TodLootDetails);
                }

                tod.MonsterName = model.Tod.MonsterName;
                tod.DayNumber = model.Tod.DayNumber;
                tod.Claim = model.Tod.Claim;
                tod.Time = model.Tod.Time.HasValue ? DateTime.SpecifyKind(model.Tod.Time.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;
                tod.Cooldown = model.Tod.Cooldown;
                tod.RepopTime = model.Tod.RepopTime.HasValue ? DateTime.SpecifyKind(model.Tod.RepopTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;
                tod.Interval = model.Tod.Interval;
                tod.LinkshellId = model.Tod.LinkshellId;
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            model.Linkshells = _context.Linkshells.ToList(); // Ensure Linkshells are populated in case of validation errors
            return View(model);
        }

        [HttpPost]
        public IActionResult EditLootDetails(TodManagerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var tod = _context.Tods.Include(t => t.TodLootDetails).FirstOrDefault(t => t.Id == model.Tod.Id);
                if (tod == null)
                {
                    return NotFound();
                }

                // Remove existing loot details
                _context.TodLootDetails.RemoveRange(tod.TodLootDetails);

                // Add updated loot details
                foreach (var lootDetail in model.TodLootDetails)
                {
                    lootDetail.TodId = tod.Id;
                    _context.TodLootDetails.Add(lootDetail);
                }

                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View("Index", model);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var tod = _context.Tods.Include(t => t.TodLootDetails).FirstOrDefault(t => t.Id == id);
            if (tod == null)
            {
                return NotFound();
            }

            // Remove related TodLootDetails records
            _context.TodLootDetails.RemoveRange(tod.TodLootDetails);

            // Remove the Tod record
            _context.Tods.Remove(tod);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}