using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinkshellManager.ViewModels;
using LinkshellManager.Models;
using LinkshellManager.Data;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using NodaTime;
using NodaTime.TimeZones;
namespace LinkshellManager.Controllers
{
    public class EventController : Controller
    { 

        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventController> _logger;
        private readonly IDateTimeZoneProvider _dateTimeZoneProvider;
        public EventController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<EventController> logger, IDateTimeZoneProvider dateTimeZoneProvider)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _dateTimeZoneProvider = dateTimeZoneProvider;
        }

[HttpGet]
public async Task<IActionResult> Index()
{
    var user = await _userManager.GetUserAsync(User);
    ViewBag.CharacterName = user?.CharacterName;

    // Get the user's time zone from the AppUser model
    var timeZoneId = user?.TimeZone;

    // Log the timeZoneId value
    _logger.LogInformation("User's time zone ID: {TimeZoneId}", timeZoneId);

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

    var primaryLinkshellId = user?.PrimaryLinkshellId;

    var events = await _context.Events
        .Include(e => e.AppUserEvents)
        .ThenInclude(aue => aue.AppUser)
        .Include(e => e.Jobs)
        .Where(e => e.LinkshellId == primaryLinkshellId)
        .ToListAsync();

    var eventViewModels = events.Select(e => new EventViewModel
    {
        Event = new Event
        {
            Id = e.Id,
            LinkshellId = e.LinkshellId,
            EventName = e.EventName,
            EventType = e.EventType,
            EventLocation = e.EventLocation,
            StartTime = e.StartTime.HasValue ? Instant.FromDateTimeUtc(DateTime.SpecifyKind(e.StartTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified() : (DateTime?)null,
            EndTime = e.EndTime.HasValue ? Instant.FromDateTimeUtc(DateTime.SpecifyKind(e.EndTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified() : (DateTime?)null,
            Duration = e.Duration,
            DkpPerHour = e.DkpPerHour,
            Details = e.Details,
            CreatorUserId = e.CreatorUserId,
            CommencementStartTime = e.CommencementStartTime
        },
        Jobs = e.Jobs.ToList(),
        AppUserEvents = e.AppUserEvents.ToList(),
        CommencementStartTime = e.CommencementStartTime,
        CreatorCharacterName = _context.Users.FirstOrDefault(u => u.Id == e.CreatorUserId)?.CharacterName
    }).ToList();

    return View(eventViewModels);
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

    var viewModel = new EventViewModel
    {
        Linkshells = userLinkshells,
        Jobs = await _context.Jobs.ToListAsync(),
        LinkshellId = user.PrimaryLinkshellId ?? 0 // Assuming PrimaryLinkshellId is nullable
    };

    return View(viewModel);
}

        [HttpPost]
        public async Task<IActionResult> Create(EventViewModel eventViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(eventViewModel);
            }

            var linkshell = _context.Linkshells.Find(eventViewModel.Event.LinkshellId);
            if (linkshell == null)
            {
                ModelState.AddModelError("Event.LinkshellId", "Invalid LinkshellId.");
                return View(eventViewModel);
            }

            var userId = _userManager.GetUserId(User); // Get the current user's ID

            // Assume the input is in local time and convert to UTC
            var startTimeUtc = eventViewModel.Event.StartTime.HasValue ? DateTime.SpecifyKind(eventViewModel.Event.StartTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;
            var endTimeUtc = eventViewModel.Event.EndTime.HasValue ? DateTime.SpecifyKind(eventViewModel.Event.EndTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;

            var newEvent = new Event
            {
                LinkshellId = eventViewModel.Event.LinkshellId,
                EventName = eventViewModel.Event.EventName,
                EventType = eventViewModel.Event.EventType,
                EventLocation = eventViewModel.Event.EventLocation,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                Duration = eventViewModel.Event.Duration,
                DkpPerHour = eventViewModel.Event.DkpPerHour,
                Details = eventViewModel.Event.Details,
                CreatorUserId = userId,
                EventDkp = eventViewModel.Event.EventDkp,
                TimeStamp = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            if (eventViewModel.Jobs != null && eventViewModel.Jobs.Any())
            {
                foreach (var job in eventViewModel.Jobs)
                {
                    job.EventId = newEvent.Id;
                    _context.Jobs.Add(job);
                }

                await _context.SaveChangesAsync();
            }

            // Add notification
            var notification = new Notification
            {
                NotificationType = "Event",
                NotificationDetails = $"New event '{newEvent.EventName}' has been created.",
                CreatedAt = DateTime.UtcNow,
                AppUserId = userId // Assuming the notification is for the user who created the event
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
                
        [HttpGet]
        public async Task<IActionResult> EditAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User is not authenticated.");
                return Challenge(); // Redirect to login if user is not authenticated
            }

            var eventToUpdate = _context.Events.Include(e => e.Jobs).FirstOrDefault(e => e.Id == id);
            if (eventToUpdate == null)
            {
                return NotFound();
            }

            var userLinkshells = await _context.AppUserLinkshells
                .Where(ul => ul.AppUserId == user.Id)
                .Select(ul => ul.Linkshell)
                .ToListAsync();

            var eventViewModel = new EventViewModel
            {
                Event = new Event
                {
                    Id = eventToUpdate.Id,
                    LinkshellId = eventToUpdate.LinkshellId,
                    EventName = eventToUpdate.EventName,
                    EventType = eventToUpdate.EventType,
                    EventLocation = eventToUpdate.EventLocation,
                    StartTime = eventToUpdate.StartTime,
                    EndTime = eventToUpdate.EndTime,
                    Duration = eventToUpdate.Duration,
                    DkpPerHour = eventToUpdate.DkpPerHour,
                    Details = eventToUpdate.Details
                },
                Jobs = eventToUpdate.Jobs.ToList(),
                Linkshells = userLinkshells
            };

            return View(eventViewModel);
        }

        [HttpPost]
        public IActionResult Edit(EventViewModel eventViewModel, int id)
        {
            if (!ModelState.IsValid)
            {
                return View(eventViewModel);
            }

            var eventToUpdate = _context.Events.Include(e => e.Jobs).FirstOrDefault(e => e.Id == id);
            if (eventToUpdate == null)
            {
                return NotFound();
            }
            var userId = _userManager.GetUserId(User);
            var linkshell = _context.Linkshells.Find(eventViewModel.Event.LinkshellId);
            if (linkshell == null)
            {
                ModelState.AddModelError("Event.LinkshellId", "Invalid LinkshellId.");
                return View(eventViewModel);
            }

            // Assume the input is in local time and convert to UTC
            var startTimeUtc = eventViewModel.Event.StartTime.HasValue ? DateTime.SpecifyKind(eventViewModel.Event.StartTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;
            var endTimeUtc = eventViewModel.Event.EndTime.HasValue ? DateTime.SpecifyKind(eventViewModel.Event.EndTime.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;

            eventToUpdate.LinkshellId = eventViewModel.Event.LinkshellId;
            eventToUpdate.EventName = eventViewModel.Event.EventName;
            eventToUpdate.EventType = eventViewModel.Event.EventType;
            eventToUpdate.EventLocation = eventViewModel.Event.EventLocation;
            eventToUpdate.StartTime = startTimeUtc;
            eventToUpdate.EndTime = endTimeUtc;
            eventToUpdate.Duration = eventViewModel.Event.Duration;
            eventToUpdate.DkpPerHour = eventViewModel.Event.DkpPerHour;
            eventToUpdate.Details = eventViewModel.Event.Details;
            eventToUpdate.CreatorUserId = userId;

            _context.Events.Update(eventToUpdate);
            _context.SaveChanges();

            // Update jobs
            var existingJobs = _context.Jobs.Where(j => j.EventId == eventToUpdate.Id).ToList();
            _context.Jobs.RemoveRange(existingJobs);
            _context.SaveChanges();

            if (eventViewModel.Jobs != null && eventViewModel.Jobs.Any())
            {
                foreach (var job in eventViewModel.Jobs)
                {
                    job.EventId = eventToUpdate.Id;
                    _context.Jobs.Add(job);
                }

                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(int jobId, int eventId)
        {
            var user = await _userManager.GetUserAsync(User);
            var characterName = user?.CharacterName;

            if (characterName == null)
            {
                return BadRequest("User does not have a character name.");
            }

            var job = _context.Jobs.FirstOrDefault(j => j.Id == jobId && j.EventId == eventId);

            if (job == null)
            {
                return NotFound("Job not found.");
            }

            var appUserEvent = _context.AppUserEvents
                .FirstOrDefault(aue => aue.AppUserId == user.Id && aue.EventId == eventId);

            if (appUserEvent == null)
            {
                appUserEvent = new AppUserEvent
                {
                    AppUserId = user.Id,
                    EventId = eventId,
                    CharacterName = characterName,
                    JobName = job.JobName,
                    SubJobName = job.SubJobName,
                    JobType = job.JobType,
                    StartTime = null, // Set StartTime to null initially
                    EventDkp = 0 // Initialize EventDkp to 0 or any default value
                };
                _context.AppUserEvents.Add(appUserEvent);
            }

            if (job.Enlisted == null)
            {
                job.Enlisted = new List<string>();
            }

            if (!job.Enlisted.Contains(characterName))
            {
                job.Enlisted.Add(characterName);
                job.SignedUp = job.Enlisted.Count;
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { id = eventId });
        }

        [HttpPost]
        public async Task<IActionResult> Unsign(int jobId, int eventId)
        {
            var user = await _userManager.GetUserAsync(User);
            var characterName = user?.CharacterName;

            if (characterName == null)
            {
                return BadRequest("User does not have a character name.");
            }

            var job = _context.Jobs.FirstOrDefault(j => j.Id == jobId && j.EventId == eventId);

            if (job == null)
            {
                return NotFound("Job not found.");
            }

            var appUserEvent = _context.AppUserEvents
                .FirstOrDefault(aue => aue.AppUserId == user.Id && aue.EventId == eventId);

            if (appUserEvent != null)
            {
                _context.AppUserEvents.Remove(appUserEvent);
            }

            if (job.Enlisted != null && job.Enlisted.Contains(characterName))
            {
                job.Enlisted.Remove(characterName);
                job.SignedUp = job.Enlisted.Count;
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { id = eventId });
        }

        [HttpPost]
        public async Task<IActionResult> StartEvent(int eventId)
        {
            var eventToStart = await _context.Events
                .Include(e => e.Jobs)
                .Include(e => e.AppUserEvents)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventToStart == null)
            {
                return NotFound();
            }

            // Set the CommencementStartTime to DateTime.UtcNow if not already set
            if (eventToStart.CommencementStartTime == null)
            {
                eventToStart.CommencementStartTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                Console.WriteLine($"CommencementStartTime set to: {eventToStart.CommencementStartTime}");
            }

            // Set the StartTime for each user to the event's CommencementStartTime in UTC
            foreach (var userEvent in eventToStart.AppUserEvents)
            {
                userEvent.StartTime = DateTime.SpecifyKind(eventToStart.CommencementStartTime.Value, DateTimeKind.Utc);
                Console.WriteLine($"User {userEvent.CharacterName} StartTime set to: {userEvent.StartTime}");
            }

            // Save changes to the database
            await _context.SaveChangesAsync();
            Console.WriteLine("Changes saved to the database.");

            return RedirectToAction(nameof(Start), new { eventId });
        }

        public DateTime ConvertUtcToUserTimeZone(DateTime utcDateTime, string timeZoneId)
        {
            var userTimeZone = DateTimeZoneProviders.Tzdb[timeZoneId];
            var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
            return instant.InZone(userTimeZone).ToDateTimeUnspecified();
        }

        [HttpGet]
        public async Task<IActionResult> Start(int eventId)
        {
            var eventToStart = await _context.Events
                .Include(e => e.Jobs)
                .Include(e => e.AppUserEvents)
                .Include(e => e.EventLootDetails)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventToStart == null)
            {
                return NotFound();
            }

            // Fetch character names of the linkshell members
            var linkshellMembers = await _context.AppUserEvents
                .Where(aue => aue.EventId == eventId)
                .Select(aue => aue.CharacterName)
                .ToListAsync();

            // Filter users who are signed up for the event
            var signedUpUsers = await _context.AppUserEvents
                .Include(aue => aue.AppUser)
                .Where(aue => aue.EventId == eventId)
                .ToListAsync();

            // Get the current user
            var user = await _userManager.GetUserAsync(User);

            // Get the user's time zone from the AppUser model
            var timeZoneId = user?.TimeZone;

            // Log the timeZoneId value
            _logger.LogInformation("User's time zone ID: {TimeZoneId}", timeZoneId);

            DateTimeZone userTimeZone;
            if (!string.IsNullOrEmpty(timeZoneId))
            {
                try
                {
                    userTimeZone = DateTimeZoneProviders.Tzdb[timeZoneId];
                }
                catch (DateTimeZoneNotFoundException)
                {
                    _logger.LogWarning("Time zone ID not found. Using UTC.");
                    userTimeZone = DateTimeZone.Utc;
                }
            }
            else
            {
                _logger.LogWarning("Time zone ID is null. Using UTC.");
                userTimeZone = DateTimeZone.Utc;
            }

            // Convert StartTime from UTC to the user's timezone
            foreach (var userEvent in signedUpUsers)
            {
                if (userEvent.StartTime.HasValue)
                {
                    userEvent.StartTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(userEvent.StartTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified();
                }
            }

            // Convert CommencementStartTime from UTC to the user's timezone
            DateTime? commencementStartTime = null;
            if (eventToStart.CommencementStartTime.HasValue)
            {
                commencementStartTime = Instant.FromDateTimeUtc(DateTime.SpecifyKind(eventToStart.CommencementStartTime.Value, DateTimeKind.Utc)).InZone(userTimeZone).ToDateTimeUnspecified();
            }

            var model = new EventViewModel
            {
                Event = eventToStart,
                Jobs = eventToStart.Jobs.ToList(),
                AppUserEvents = signedUpUsers,
                CommencementStartTime = commencementStartTime,
                EventLootDetails = eventToStart.EventLootDetails.ToList(),
                LinkshellMembers = linkshellMembers
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CancelEvent(int eventId)
        {
            var eventToDelete = await _context.Events.FindAsync(eventId);
            if (eventToDelete == null)
            {
                return NotFound();
            }

            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> QuickJoin(int eventId, string characterName, string jobName, string subJobName, string jobType)
        {

            var user = await _userManager.GetUserAsync(User);

            var eventToJoin = await _context.Events
                .Include(e => e.AppUserEvents)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventToJoin == null)
            {
                return NotFound();
            }

            // Check if the user is already signed up
            var existingUserEvent = eventToJoin.AppUserEvents.FirstOrDefault(aue => aue.CharacterName == characterName);
            if (existingUserEvent == null)
            {
                // Add the user to the event
                var appUserEvent = new AppUserEvent
                {
                    AppUserId = user.Id,
                    EventId = eventId,
                    CharacterName = characterName,
                    JobName = jobName,
                    SubJobName = subJobName,
                    JobType = jobType,
                    StartTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local).ToUniversalTime(),
                    EventDkp = 0,
                    IsQuickJoin = true
                };
                _context.AppUserEvents.Add(appUserEvent);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Start), new { eventId });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmAttendance(int eventId, Dictionary<string, string> attendance)
        {
            Console.WriteLine("EventId: " + eventId);
            var eventEntity = await _context.Events.Include(e => e.AppUserEvents).FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
            {
                return NotFound();
            }

            foreach (var userEvent in eventEntity.AppUserEvents)
            {
                if (attendance.TryGetValue($"attendance_{userEvent.CharacterName}", out var status))
                {
                    if (status == "deny")
                    {
                        _context.AppUserEvents.Remove(userEvent);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Call the StartEvent method
            await StartEvent(eventId);

            return RedirectToAction("Start", new { eventId });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyUser(int eventId, string characterName, bool isVerified)
        {
            var userEvent = await _context.AppUserEvents
                .FirstOrDefaultAsync(aue => aue.CharacterName == characterName && aue.EventId == eventId);

            if (userEvent == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var proctorCharacterName = user?.CharacterName;

            userEvent.IsVerified = isVerified;
            userEvent.Proctor = proctorCharacterName; // Set the Proctor field

            await _context.SaveChangesAsync();

            return RedirectToAction("Start", new { eventId = userEvent.EventId });
        }

        [HttpPost]
        public async Task<IActionResult> UndoVerification(int eventId, string characterName)
        {
            var userEvent = await _context.AppUserEvents
                .FirstOrDefaultAsync(aue => aue.CharacterName == characterName && aue.EventId == eventId);

            if (userEvent == null)
            {
                return NotFound();
            }

            userEvent.IsVerified = null;
            userEvent.Proctor = null;

            await _context.SaveChangesAsync();

            return RedirectToAction("Start", new { eventId = userEvent.EventId });
        }

        [HttpPost]
        public async Task<IActionResult> PauseUser(string characterName)
        {
            var userEvent = await _context.AppUserEvents.FirstOrDefaultAsync(aue => aue.CharacterName == characterName);
            if (userEvent != null)
            {
                userEvent.IsOnBreak = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Start", new { eventId = userEvent.EventId });
        }

        [HttpPost]
        public async Task<IActionResult> ResumeUser(string characterName)
        {
            var userEvent = await _context.AppUserEvents.FirstOrDefaultAsync(aue => aue.CharacterName == characterName);
            if (userEvent != null)
            {
                userEvent.IsOnBreak = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Start", new { eventId = userEvent.EventId });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitLootDetails(int eventId, string itemName, string itemWinner, int winningDkpSpent)
        {
            var eventEntity = await _context.Events.Include(e => e.EventLootDetails).FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
            {
                return NotFound();
            }

            var lootDetail = new EventLootDetail
            {
                EventId = eventId,
                ItemName = itemName,
                ItemWinner = itemWinner,
                WinningDkpSpent = winningDkpSpent
            };

            _context.EventLootDetails.Add(lootDetail);

            // Subtract DKP from the item winner
            if (!string.IsNullOrEmpty(itemWinner))
            {
                var winner = await _context.AppUserLinkshells
                    .FirstOrDefaultAsync(ul => ul.CharacterName == itemWinner);

                if (winner != null)
                {
                    var previousDkp = winner.LinkshellDkp ?? 0;
                    winner.LinkshellDkp -= winningDkpSpent;
                    _context.Update(winner);

                    // Add entry to DkpLedger
                    var dkpLedger = new DkpLedger
                    {
                        AppUserLinkshellId = winner.Id,
                        DkpType = "Loot",
                        TransactionType = "Subtract",
                        TransactionValue = winningDkpSpent,
                        PreviousDkp = previousDkp,
                        NewDkp = winner.LinkshellDkp
                    };
                    _context.DkpLedgers.Add(dkpLedger);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Start), new { eventId });
        }

        [HttpPost]
        public async Task<IActionResult> EndEvent(int eventId, Dictionary<string, double> dkpValues, double eventDkp, double eventDuration, Dictionary<string, double> durationValues)
        {
            var currentEvent = await _context.Events
                .Include(e => e.AppUserEvents)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (currentEvent == null)
            {
                return NotFound();
            }

            // Update the EventDkp and Duration values
            currentEvent.EventDkp = RoundToNearestQuarter(eventDkp); // Ensure consistent rounding
            currentEvent.Duration = RoundToNearestQuarter(eventDuration); // Ensure consistent rounding

            foreach (var appUserEvent in currentEvent.AppUserEvents)
            {
                if (dkpValues.TryGetValue(appUserEvent.CharacterName, out var dkp))
                {
                    appUserEvent.EventDkp = RoundToNearestQuarter(dkp); // Ensure consistent rounding
                    Console.WriteLine($"Character: {appUserEvent.CharacterName}, DKP: {dkp}");
                }

                if (durationValues.TryGetValue(appUserEvent.CharacterName, out var duration))
                {
                    appUserEvent.Duration = RoundToNearestQuarter(duration); // Ensure consistent rounding
                    Console.WriteLine($"Character: {appUserEvent.CharacterName}, Duration: {appUserEvent.Duration}");
                }
            }

            var eventHistory = new EventHistory
            {
                LinkshellId = currentEvent.LinkshellId,
                EventName = currentEvent.EventName,
                EventType = currentEvent.EventType,
                EventLocation = currentEvent.EventLocation,
                StartTime = currentEvent.StartTime,
                EndTime = DateTime.Now,
                CommencementStartTime = currentEvent.CommencementStartTime,
                Duration = currentEvent.Duration,
                DkpPerHour = currentEvent.DkpPerHour,
                EventDkp = currentEvent.EventDkp,
                Details = currentEvent.Details,
                TimeStamp = DateTime.UtcNow,
                AppUserEventHistories = currentEvent.AppUserEvents.Select(aue => new AppUserEventHistory
                {
                    AppUserId = aue.AppUserId,
                    CharacterName = aue.CharacterName,
                    JobName = aue.JobName,
                    SubJobName = aue.SubJobName,
                    JobType = aue.JobType,
                    StartTime = aue.StartTime,
                    Duration = aue.Duration,
                    EventDkp = aue.EventDkp,
                    IsQuickJoin = aue.IsQuickJoin,
                    IsVerified = aue.IsVerified,
                    Proctor = aue.Proctor
                }).ToList()
            };

            _context.EventHistories.Add(eventHistory);

            // Update LinkshellDkp for each user in the event
            foreach (var appUserEvent in currentEvent.AppUserEvents)
            {
                var appUserLinkshell = await _context.AppUserLinkshells
                    .FirstOrDefaultAsync(aul => aul.AppUserId == appUserEvent.AppUserId && aul.LinkshellId == currentEvent.LinkshellId);

                if (appUserLinkshell == null)
                {
                    // Create a new AppUserLinkshell entry if it doesn't exist
                    appUserLinkshell = new AppUserLinkshell
                    {
                        AppUserId = appUserEvent.AppUserId,
                        LinkshellId = currentEvent.LinkshellId,
                        LinkshellDkp = appUserEvent.EventDkp ?? 0
                    };
                    _context.AppUserLinkshells.Add(appUserLinkshell);
                }
                else
                {
                    // Update the existing AppUserLinkshell entry
                    var previousDkp = appUserLinkshell.LinkshellDkp ?? 0;
                    appUserLinkshell.LinkshellDkp = (appUserLinkshell.LinkshellDkp ?? 0) + (appUserEvent.EventDkp ?? 0);

                    // Add entry to DkpLedger
                    var dkpLedger = new DkpLedger
                    {
                        AppUserLinkshellId = appUserLinkshell.Id,
                        DkpType = "Event",
                        TransactionType = "Add",
                        TransactionValue = appUserEvent.EventDkp ?? 0,
                        PreviousDkp = previousDkp,
                        NewDkp = appUserLinkshell.LinkshellDkp
                    };
                    _context.DkpLedgers.Add(dkpLedger);
                }
            }

            _context.Events.Remove(currentEvent); // Optionally remove the current event
            await _context.SaveChangesAsync(); // Optionally keep the current event for testing

            return RedirectToAction("Index", "Dashboard"); // Redirect to a relevant page
        }

        private double RoundToNearestQuarter(double value)
        {
            return Math.Round(value * 4) / 4;
        }
        
    }
}