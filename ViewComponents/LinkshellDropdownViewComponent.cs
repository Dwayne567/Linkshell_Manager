// File: ViewComponents/LinkshellDropdownViewComponent.cs
using Microsoft.AspNetCore.Mvc;
using LinkshellManager.Data;
using LinkshellManager.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LinkshellManager.ViewModels;

public class LinkshellDropdownViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public LinkshellDropdownViewComponent(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
        var userLinkshells = await _context.AppUserLinkshells
            .Where(aul => aul.AppUserId == user.Id)
            .Select(aul => aul.Linkshell)
            .ToListAsync();

        var selectedLinkshell = userLinkshells
            .FirstOrDefault(l => l.Id == user.PrimaryLinkshellId);

        var model = new SettingsViewModel
        {
            Linkshells = userLinkshells,
            SelectedLinkshellId = selectedLinkshell?.Id
        };

        return View(model);
    }
}