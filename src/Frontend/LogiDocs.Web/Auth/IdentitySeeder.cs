using Microsoft.AspNetCore.Identity;

namespace LogiDocs.Web.Auth;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();

        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new[]
        {
            Roles.Shipper,
            Roles.Carrier,
            Roles.CustomsBroker,
            Roles.CustomsAuthority,
            Roles.Administrator
        };

        foreach (var r in roles)
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new IdentityRole<Guid>(r));

        await EnsureUser(userMgr, "shipper@demo.com", "Test1234", Roles.Shipper);
        await EnsureUser(userMgr, "carrier@demo.com", "Test1234", Roles.Carrier);
        await EnsureUser(userMgr, "broker@demo.com", "Test1234", Roles.CustomsBroker);
        await EnsureUser(userMgr, "authority@demo.com", "Test1234", Roles.CustomsAuthority);
        await EnsureUser(userMgr, "admin@demo.com", "Test1234", Roles.Administrator);
    }

    private static async Task EnsureUser(
        UserManager<ApplicationUser> userMgr,
        string email,
        string password,
        string role)
    {
        var u = await userMgr.FindByEmailAsync(email);
        if (u == null)
        {
            u = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var created = await userMgr.CreateAsync(u, password);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join("; ",
                    created.Errors.Select(e => e.Description)));
        }

        if (!await userMgr.IsInRoleAsync(u, role))
            await userMgr.AddToRoleAsync(u, role);
    }
}