using Microsoft.AspNetCore.Identity;
using namera_API.Constants.Identity;
using namera_API.Models.Identity;

namespace namera_API.Data.Seed;

public static class IdentitySeeder
{
    public const string OwnerPhoneNumber = "0590000000";
    public const string OwnerPassword = "Owner123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleAsync(roleManager, AppRoles.Customer);
        await EnsureRoleAsync(roleManager, AppRoles.Owner);

        var owner = await userManager.FindByNameAsync(OwnerPhoneNumber);

        if (owner is null)
        {
            owner = new ApplicationUser
            {
                FirstName = "Namira",
                LastName = "Owner",
                Address = "Namira",
                UserName = OwnerPhoneNumber,
                PhoneNumber = OwnerPhoneNumber,
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(owner, OwnerPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed owner account: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(owner, AppRoles.Owner))
        {
            await userManager.AddToRoleAsync(owner, AppRoles.Owner);
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
