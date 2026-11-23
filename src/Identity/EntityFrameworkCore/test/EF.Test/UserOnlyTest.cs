// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class UserOnlyTest : IClassFixture<ScratchDatabaseFixture>
{
    private readonly ApplicationBuilder _builder;

    public class TestUserDbContext : IdentityUserContext<IdentityUser>
    {
        public TestUserDbContext(DbContextOptions options) : base(options) { }
    }

    public UserOnlyTest(ScratchDatabaseFixture fixture)
    {
        var services = new ServiceCollection();

        services
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddDbContext<TestUserDbContext>(
                o => o.UseSqlite(fixture.Connection)
                    .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)))
            .AddIdentityCore<IdentityUser>(o => { })
            .AddEntityFrameworkStores<TestUserDbContext>();

        services.AddLogging();

        var provider = services.BuildServiceProvider();
        _builder = new ApplicationBuilder(provider);

        using (var scoped = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
        using (var db = scoped.ServiceProvider.GetRequiredService<TestUserDbContext>())
        {
            db.Database.EnsureCreated();
        }
    }

    [ConditionalFact]
    public async Task EnsureStartupUsageWorks()
    {
        var userStore = _builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
        var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

        Assert.NotNull(userStore);
        Assert.NotNull(userManager);

        const string userName = "admin";
        const string password = "[PLACEHOLDER]-1a";
        var user = new IdentityUser { UserName = userName };
        IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
        IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
    }

    [ConditionalFact]
    public async Task ConcurrentAccessFailuresArePersisted()
    {
        var scopeFactory = _builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
        var user = new IdentityUser { UserName = Guid.NewGuid().ToString(), LockoutEnabled = true };
        using (var scope = scopeFactory.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        }

        using (var scope1 = scopeFactory.CreateScope())
        using (var scope2 = scopeFactory.CreateScope())
        {
            var manager1 = scope1.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var manager2 = scope2.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user1 = await manager1.FindByIdAsync(user.Id);
            var user2 = await manager2.FindByIdAsync(user.Id);
            Assert.NotNull(user1);
            Assert.NotNull(user2);

            IdentityResultAssert.IsSuccess(await manager1.AccessFailedAsync(user1));
            IdentityResultAssert.IsSuccess(await manager2.AccessFailedAsync(user2));
        }

        using (var scope = scopeFactory.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var persistedUser = await manager.FindByIdAsync(user.Id);
            Assert.NotNull(persistedUser);
            Assert.Equal(2, persistedUser.AccessFailedCount);
        }
    }

    [ConditionalFact]
    public async Task FindByEmailThrowsWithTwoUsersWithSameEmail()
    {
        var userStore = _builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
        var manager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

        Assert.NotNull(userStore);
        Assert.NotNull(manager);

        var userA = new IdentityUser(Guid.NewGuid().ToString());
        userA.Email = "dupe@dupe.com";
        const string password = "[PLACEHOLDER]-1a";
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(userA, password));
        var userB = new IdentityUser(Guid.NewGuid().ToString());
        userB.Email = "dupe@dupe.com";
        IdentityResultAssert.IsSuccess(await manager.CreateAsync(userB, password));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.FindByEmailAsync("dupe@dupe.com"));
    }
}
