using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Claims;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.AntiVirus;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Services;

namespace Tavstal.YggdrasilSharp.Tests.Models;

/// <summary>
/// Base class for controller tests: builds an in-memory database, a user store, a user manager, a
/// request context and two mock users, and exposes helpers to authenticate a controller request.
/// </summary>
public abstract class ControllerTestBase
{
    /// <summary>
    /// The output helper used to write test diagnostics.
    /// </summary>
    protected readonly ITestOutputHelper _testOutputHelper;

    /// <summary>
    /// The helper owning the test services shared by the test class.
    /// </summary>
    protected readonly TestHelper _testHelper;

    /// <summary>
    /// The in-memory database holding the data of the test.
    /// </summary>
    protected readonly CustomDbContext _dbContext;

    /// <summary>
    /// The user store backed by <see cref="_dbContext"/>.
    /// </summary>
    protected readonly CustomUserStore _userStore;

    /// <summary>
    /// The user manager used to create users and password hashes.
    /// </summary>
    protected readonly CustomUserManager _userManager;

    /// <summary>
    /// The hasher used to create and verify the password hashes of the mock users.
    /// </summary>
    protected readonly IPasswordHasher<CustomUser> _passwordHasher;

    /// <summary>
    /// The request context given to the controller under test.
    /// </summary>
    protected readonly DefaultHttpContext _controllerHttpContext;

    /// <summary>
    /// The memory cache service scoped to the test instance.
    /// </summary>
    protected readonly MemoryCacheService _memoryCacheService;

    /// <summary>
    /// The email service capturing the emails produced by the test.
    /// </summary>
    protected readonly FakeEmailService _fakeEmailService;

    /// <summary>
    /// The AntiVirus service given to the controller under test. Unreachable daemon connections are
    /// swallowed by the service, so no AntiVirus instance is needed for the tests to pass.
    /// </summary>
    protected readonly IAntiVirusService AntiVirusService;

    /// <summary>
    /// The test configuration given to the controller and services under test.
    /// </summary>
    protected readonly AppConfiguration AppConfiguration;

    /// <summary>
    /// The default mock user, confirmed and with a known password hash.
    /// </summary>
    protected readonly CustomUser _userMock;

    /// <summary>
    /// The second mock user, kept unconfirmed to test the confirmation flows.
    /// </summary>
    protected readonly CustomUser _userMock2;

    /// <summary>
    /// The plain password matching the password hashes of the mock users.
    /// </summary>
    protected const string _passwordMock = "This%Valid_And#Pass%mock-2026";

    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerTestBase"/> class with fresh,
    /// non-shared test services so parallel test classes cannot interfere with each other.
    /// </summary>
    /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
    protected ControllerTestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // Fresh, non-shared test services per test instance so parallel test classes cannot
        // interfere with each other's caches, email records or upload directories.
        _testHelper = new TestHelper();

        _dbContext = TestHelper.CreateInMemoryDbContext();
        _userStore = TestHelper.CreateCustomUserStore(_dbContext);
        _userManager = _testHelper.CreateCustomUserManager(_dbContext, _userStore);
        _passwordHasher = _testHelper.PasswordHasher;
        _memoryCacheService = _testHelper.MemoryCacheService;
        _fakeEmailService = _testHelper.FakeEmailService;
        AntiVirusService = _testHelper.AntiVirusService;
        AppConfiguration = _testHelper.Settings;

        var uploadTempDir = Path.Combine(Path.GetTempPath(), "ysharp-tests-uploads");
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["UploadDirectory"] = uploadTempDir
        }).Build();
        Program.UploadDir = uploadTempDir;
        Program.IsDevelopment = true;

        _controllerHttpContext = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse(TestHelper.IpAddress) }
        };
        _controllerHttpContext.Request.Headers.UserAgent = TestHelper.UserAgent;
        _controllerHttpContext.Request.Host = new HostString("localhost", 5000);

        _userMock = new CustomUser
        {
            Email = "testuser@gmail.com",
            EmailConfirmed = true,
            NormalizedEmail = "testuser@gmail.com".Normalize(),
            UserName = "testuser",
            NormalizedUserName = "testuser".Normalize(),
            PasswordHash = "",
            CreateDate = DateTimeOffset.UtcNow,
            LastLogin = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
            SkinModel = ESkinType.WIDE
        };
        _userMock.PasswordHash = _passwordHasher.HashPassword(_userMock, _passwordMock);
        _userMock2 = new CustomUser
        {
            Email = "testuser2@gmail.com",
            EmailConfirmed = true,
            NormalizedEmail = "testuser2@gmail.com".Normalize(),
            UserName = "testuser2",
            NormalizedUserName = "testuser2".Normalize(),
            PasswordHash = "",
            CreateDate = DateTimeOffset.UtcNow,
            LastLogin = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
            SkinModel = ESkinType.WIDE,
            LockoutEnabled = false,
        };
        _userMock2.PasswordHash = _passwordHasher.HashPassword(_userMock2, _passwordMock);
    }

    /// <summary>
    /// Persists a user together with the default and, optionally, the admin role, then authenticates
    /// the given controller with the created user.
    /// </summary>
    /// <param name="controller">The controller whose <see cref="ControllerContext"/> receives the authenticated request.</param>
    /// <param name="user">The user to create, or <c>null</c> to use the default mock user.</param>
    /// <param name="givePermissions">Whether the user also receives the admin role and its claims.</param>
    /// <returns>The created and persisted user.</returns>
    protected async Task<CustomUser> CreateUserAsync(Controller controller, CustomUser? user = null, bool givePermissions = true)
    {
        user = await _userStore.AddUserAsync(user ?? _userMock, true);
        if (givePermissions)
        {
            var adminRole = await _userStore.Roles.AddAsync(new CustomRole(3, "admin", "ADMIN"), true);
            await _userStore.UserRoles.AddAsync(new CustomUserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id
            }, true);

            var adminClaims = CustomRoleClaims.Claims["Admin"];
            foreach (var claim in adminClaims)
                await _userStore.RoleClaims.AddAsync(new IdentityRoleClaim<string>()
                {
                    RoleId = adminRole.Id,
                    ClaimType = claim.Key,
                    ClaimValue = claim.DefaultValue
                }, true);
        }

        var role = await _userStore.Roles.AddAsync(new CustomRole(1, "default", "DEFAULT"), true);
        await _userStore.UserRoles.AddAsync(new CustomUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        }, true);

        var defaultClaims = CustomRoleClaims.Claims["Default"];
        foreach (var claim in defaultClaims)
            await _userStore.RoleClaims.AddAsync(new IdentityRoleClaim<string>()
            {
                RoleId = role.Id,
                ClaimType = claim.Key,
                ClaimValue = claim.DefaultValue
            }, true);
        await _dbContext.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName)
        };
        _controllerHttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        controller.ControllerContext.HttpContext = _controllerHttpContext;
        return user;
    }
}
