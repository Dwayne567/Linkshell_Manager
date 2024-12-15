using LinkshellManager.Models;
using Microsoft.AspNetCore.Identity;
using LinkshellManager.Utils;
using System.Globalization;

namespace LinkshellManager.Data
{
    public class Seed
    {

        public static async void SeedUsersAndRolesAsync(IApplicationBuilder applicationBuilder)
        {
            using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                //Roles
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
                    await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
                if (!await roleManager.RoleExistsAsync(UserRoles.User))
                    await roleManager.CreateAsync(new IdentityRole(UserRoles.User));

                //Users
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                string adminUser1Email = "admin1@test.com";

                var adminUser1 = await userManager.FindByEmailAsync(adminUser1Email);
                if (adminUser1 == null)
                {
                    var newAdmin1User = new AppUser()
                    {
                        UserName = "admin1",
                        Email = adminUser1Email,
                        EmailConfirmed = true,
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAdmin1User, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAdmin1User, UserRoles.Admin);
                    }
                }

                // --------------------------------------------------------------------------------

                string adminUser2Email = "admin2@test.com";

                var adminUser2 = await userManager.FindByEmailAsync(adminUser2Email);
                if (adminUser2 == null)
                {
                    var newAdmin2User = new AppUser()
                    {
                        UserName = "admin2",
                        Email = adminUser2Email,
                        EmailConfirmed = true,
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAdmin2User, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAdmin2User, UserRoles.Admin);
                    }
                }

                // --------------------------------------------------------------------------------

                string appUser1Email = "user1@test.com";

                var appUser1 = await userManager.FindByEmailAsync(appUser1Email);
                if (appUser1 == null)
                {
                    var newAppUser1 = new AppUser()
                    {
                        UserName = "user1",
                        Email = appUser1Email,
                        EmailConfirmed = true,
                        CharacterName = "Character1"
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAppUser1, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAppUser1, UserRoles.User);
                    }
                }

                // --------------------------------------------------------------------------------

                string appUser2Email = "user2@test.com";

                var appUser2 = await userManager.FindByEmailAsync(appUser2Email);
                if (appUser2 == null)
                {
                    var newAppUser2 = new AppUser()
                    {
                        UserName = "user2",
                        Email = appUser2Email,
                        EmailConfirmed = true,
                        CharacterName = "Character2"
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAppUser2, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAppUser2, UserRoles.User);
                    }
                }

                // --------------------------------------------------------------------------------

                string appUser3Email = "user3@test.com";

                var appUser3 = await userManager.FindByEmailAsync(appUser3Email);
                if (appUser3 == null)
                {
                    var newAppUser3 = new AppUser()
                    {
                        UserName = "user3",
                        Email = appUser3Email,
                        EmailConfirmed = true,
                        CharacterName = "Character3"
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAppUser3, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAppUser3, UserRoles.User);
                    }
                }

                // --------------------------------------------------------------------------------

                string appUser4Email = "user4@test.com";

                var appUser4 = await userManager.FindByEmailAsync(appUser4Email);
                if (appUser4 == null)
                {
                    var newAppUser4 = new AppUser()
                    {
                        UserName = "user4",
                        Email = appUser4Email,
                        EmailConfirmed = true,
                        CharacterName = "Character4"
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAppUser4, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAppUser4, UserRoles.User);
                    }
                }

                // --------------------------------------------------------------------------------

                string appUser5Email = "user5@test.com";

                var appUser5 = await userManager.FindByEmailAsync(appUser5Email);
                if (appUser5 == null)
                {
                    var newAppUser5 = new AppUser()
                    {
                        UserName = "user5",
                        Email = appUser5Email,
                        EmailConfirmed = true,
                        CharacterName = "Character5"
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAppUser5, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAppUser5, UserRoles.User);
                    }
                }

                // --------------------------------------------------------------------------------

                string appUser6Email = "user6@test.com";

                var appUser6 = await userManager.FindByEmailAsync(appUser6Email);
                if (appUser6 == null)
                {
                    var newAppUser6 = new AppUser()
                    {
                        UserName = "user6",
                        Email = appUser6Email,
                        EmailConfirmed = true,
                        CharacterName = "Character6"
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(newAppUser6, "Coding@1234?");
                    if (result.Succeeded)
                    {
                        // If user creation is successful, add to role
                        await userManager.AddToRoleAsync(newAppUser6, UserRoles.User);
                    }
                }
            }
        }
    }
}
