using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LinkshellManager.Models;
using LinkshellManager.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using LinkshellManager.ViewModels;

namespace LinkshellManager.Controllers
{
    [Authorize]
    public class LinkshellController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LinkshellController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Linkshell/Index
    public async Task<IActionResult> Index()
    {
        // Retrieve the current authenticated user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Challenge(); // Redirect to login if user is not authenticated
        }

        // Get the list of linkshells the user is a part of
        var linkshells = await _context.AppUserLinkshells
            .Where(aul => aul.AppUserId == userId)
            .Select(aul => aul.Linkshell)
            .ToListAsync();

        // Calculate totals for each linkshell
        foreach (var linkshell in linkshells)
        {
            linkshell.TotalMembers = await _context.AppUserLinkshells
                .CountAsync(aul => aul.LinkshellId == linkshell.Id);

            linkshell.TotalItems = await _context.Items
                .Where(item => item.LinkshellId == linkshell.Id)
                .SumAsync(item => item.Quantity ?? 0);

            linkshell.Revenue = await _context.Incomes
                .Where(income => income.LinkshellId == linkshell.Id)
                .SumAsync(income => income.Value);
        }

        return View(linkshells);
    }

        // GET: Linkshell/Create
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult Customize()
        {
            return View();
        }

        // POST: Linkshell/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(LinkshellViewModel model)
{
    if (ModelState.IsValid)
    {
        // Retrieve the current authenticated user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Challenge(); // Redirect to login if user is not authenticated
        }

        // Retrieve the current authenticated user's CharacterName
        var appUser = await _context.Users.FindAsync(userId);
        if (appUser == null)
        {
            return NotFound(); // User not found
        }

        var linkshell = new Linkshell
        {
            LinkshellName = model.LinkshellName,
            AppUserId = userId // Set the AppUserId
        };

        _context.Add(linkshell);
        await _context.SaveChangesAsync();

        // Add the user to the AppUserLinkshell table
        var appUserLinkshell = new AppUserLinkshell
        {
            AppUserId = userId,
            LinkshellId = linkshell.Id,
            CharacterName = appUser.CharacterName, // Set the CharacterName
            Rank = "Leader", // Set the Rank to "Leader"
            Status = "Active", // Set the Status to "Active"
            LinkshellDkp = 0 // Initialize DKP if needed
        };

        _context.Add(appUserLinkshell);
        await _context.SaveChangesAsync();

        // Set the PrimaryLinkshellId and PrimaryLinkshellName fields of the AppUser
        appUser.PrimaryLinkshellId = linkshell.Id;
        appUser.PrimaryLinkshellName = linkshell.LinkshellName;

        _context.Update(appUser);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    return View(model);
}

        // GET: Linkshell/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var linkshell = await _context.Linkshells
                .FirstOrDefaultAsync(m => m.Id == id);
            if (linkshell == null)
            {
                return NotFound();
            }

            return View(linkshell);
        }

        // GET: Linkshell/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var linkshell = await _context.Linkshells.FindAsync(id);
            if (linkshell == null)
            {
                return NotFound();
            }

            var model = new LinkshellViewModel
            {
                LinkshellName = linkshell.LinkshellName
            };

            return View(model);
        }

        // POST: Linkshell/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LinkshellViewModel model)
        {
            if (ModelState.IsValid)
            {
                var linkshell = await _context.Linkshells.FindAsync(id);
                if (linkshell == null)
                {
                    return NotFound();
                }

                linkshell.LinkshellName = model.LinkshellName;

                try
                {
                    _context.Update(linkshell);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LinkshellExists(linkshell.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Linkshell/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var linkshell = await _context.Linkshells
                .FirstOrDefaultAsync(m => m.Id == id);
            if (linkshell == null)
            {
                return NotFound();
            }

            return View(linkshell);
        }

        // POST: Linkshell/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var linkshell = await _context.Linkshells.FindAsync(id);
            _context.Linkshells.Remove(linkshell);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LinkshellExists(int id)
        {
            return _context.Linkshells.Any(e => e.Id == id);
        }


    }
}