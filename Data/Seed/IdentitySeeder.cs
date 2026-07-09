using Microsoft.AspNetCore.Identity;
using namera_API.Constants.Identity;
using namera_API.Models.Identity;

namespace namera_API.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var ownerEmail = configuration["SeedOwner:Email"];
        var ownerUserName = configuration["SeedOwner:UserName"];
        var ownerPassword = configuration["SeedOwner:Password"];

        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleAsync(roleManager, AppRoles.Customer);
        await EnsureRoleAsync(roleManager, AppRoles.Owner);

        if (string.IsNullOrWhiteSpace(ownerEmail) ||
            string.IsNullOrWhiteSpace(ownerUserName) ||
            string.IsNullOrWhiteSpace(ownerPassword))
        {
            return;
        }

        var owner = await userManager.FindByNameAsync(ownerUserName)
            ?? await userManager.FindByEmailAsync(ownerEmail);

        if (owner is null)
        {
            owner = new ApplicationUser
            {
                FirstName = "Store",
                LastName = "Owner",
                Address = "Handmade Gifts Store",
                UserName = ownerUserName,
                Email = ownerEmail,
                EmailConfirmed = true,
                PhoneNumber = "0590000000",
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(owner, ownerPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed owner account: {errors}");
            }
        }
        else
        {
            var changed = false;

            if (owner.UserName != ownerUserName)
            {
                owner.UserName = ownerUserName;
                changed = true;
            }

            if (owner.Email != ownerEmail)
            {
                owner.Email = ownerEmail;
                owner.EmailConfirmed = true;
                changed = true;
            }

            if (changed)
            {
                await userManager.UpdateAsync(owner);
            }
        }

        if (!await userManager.IsInRoleAsync(owner, AppRoles.Owner))
        {
            await userManager.AddToRoleAsync(owner, AppRoles.Owner);
        }

        if (!await userManager.CheckPasswordAsync(owner, ownerPassword))
        {
            if (!string.IsNullOrWhiteSpace(owner.PasswordHash))
            {
                await userManager.RemovePasswordAsync(owner);
            }

            var passwordResult = await userManager.AddPasswordAsync(owner, ownerPassword);

            if (!passwordResult.Succeeded)
            {
                var errors = string.Join(", ", passwordResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to set owner password: {errors}");
            }
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}
