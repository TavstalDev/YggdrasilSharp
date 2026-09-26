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
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Services;

namespace Tavstal.YggdrasilSharp.Tests.Models;

public abstract class ControllerTestBase
{
    protected readonly ITestOutputHelper _testOutputHelper;
    protected readonly TestHelper _testHelper;
    protected readonly CustomDbContext _dbContext;
    protected readonly CustomUserStore _userStore;
    protected readonly CustomUserManager _userManager;
    protected readonly IPasswordHasher<CustomUser> _passwordHasher;
    protected readonly DefaultHttpContext _controllerHttpContext;
    protected readonly MemoryCacheService _memoryCacheService;
    protected readonly FakeEmailService _fakeEmailService;
    protected readonly AppConfiguration AppConfiguration;
    protected readonly CustomUser _userMock;
    protected readonly CustomUser _userMock2;
    protected const string _passwordMock = "This%Valid_And#Pass%mock-2026";

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
        AppConfiguration = TestHelper.CreateTestSettings();

        var uploadTempDir = Path.Combine(Path.GetTempPath(), "ysharp-tests-uploads");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
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