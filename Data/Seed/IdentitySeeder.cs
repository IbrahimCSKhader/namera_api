using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using namera_API.Constants.Identity;
using namera_API.Models.Identity;

namespace namera_API.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleAsync(roleManager, AppRoles.Customer);
        await EnsureRoleAsync(roleManager, AppRoles.Owner);

        var ownerSeed = new SeedUser(
            FirstName: configuration["SeedOwner:FirstName"] ?? "Namira",
            LastName: configuration["SeedOwner:LastName"] ?? "Owner",
            UserName: configuration["SeedOwner:UserName"] ?? "namer",
            Email: configuration["SeedOwner:Email"] ?? "namera@gmail.com",
            PhoneNumber: configuration["SeedOwner:PhoneNumber"] ?? "0590000000",
            Address: configuration["SeedOwner:Address"] ?? "Namira handmade gifts store",
            Password: configuration["SeedOwner:Password"] ?? "namera12345",
            Role: AppRoles.Owner);

        var customerSeed = new SeedUser(
            FirstName: configuration["SeedCustomer:FirstName"] ?? "Demo",
            LastName: configuration["SeedCustomer:LastName"] ?? "Customer",
            UserName: configuration["SeedCustomer:UserName"] ?? "customer_demo",
            Email: configuration["SeedCustomer:Email"] ?? "customer@namera.local",
            PhoneNumber: configuration["SeedCustomer:PhoneNumber"] ?? "0591111111",
            Address: configuration["SeedCustomer:Address"] ?? "Demo customer address",
            Password: configuration["SeedCustomer:Password"] ?? "Customer12345",
            Role: AppRoles.Customer);

        await EnsureUserAsync(userManager, ownerSeed);
        await EnsureUserAsync(userManager, customerSeed);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, SeedUser seed)
    {
        var user = await userManager.FindByNameAsync(seed.UserName)
            ?? await userManager.FindByEmailAsync(seed.Email)
            ?? await userManager.Users.FirstOrDefaultAsync(item => item.PhoneNumber == seed.PhoneNumber);

        if (user is null)
        {
            user = new ApplicationUser
            {
                FirstName = seed.FirstName,
                LastName = seed.LastName,
                Address = seed.Address,
                UserName = seed.UserName,
                Email = seed.Email,
                EmailConfirmed = true,
                PhoneNumber = seed.PhoneNumber,
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(user, seed.Password);

            if (!createResult.Succeeded)
            {
                ThrowSeedFailure($"Failed to seed {seed.Role} account", createResult);
            }
        }
        else
        {
            user.FirstName = seed.FirstName;
            user.LastName = seed.LastName;
            user.Address = seed.Address;
            user.UserName = seed.UserName;
            user.Email = seed.Email;
            user.EmailConfirmed = true;
            user.PhoneNumber = seed.PhoneNumber;
            user.PhoneNumberConfirmed = true;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                ThrowSeedFailure($"Failed to update seeded {seed.Role} account", updateResult);
            }
        }

        if (!await userManager.IsInRoleAsync(user, seed.Role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, seed.Role);

            if (!roleResult.Succeeded)
            {
                ThrowSeedFailure($"Failed to assign {seed.Role} role", roleResult);
            }
        }

        if (!await userManager.CheckPasswordAsync(user, seed.Password))
        {
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                var removePasswordResult = await userManager.RemovePasswordAsync(user);

                if (!removePasswordResult.Succeeded)
                {
                    ThrowSeedFailure($"Failed to reset {seed.Role} password", removePasswordResult);
                }
            }

            var passwordResult = await userManager.AddPasswordAsync(user, seed.Password);

            if (!passwordResult.Succeeded)
            {
                ThrowSeedFailure($"Failed to set {seed.Role} password", passwordResult);
            }
        }
    }

    private static void ThrowSeedFailure(string message, IdentityResult result)
    {
        var errors = string.Join(", ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{message}: {errors}");
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private sealed record SeedUser(
        string FirstName,
        string LastName,
        string UserName,
        string Email,
        string PhoneNumber,
        string Address,
        string Password,
        string Role);
}
