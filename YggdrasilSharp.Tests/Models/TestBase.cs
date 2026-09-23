using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Models;

public abstract class TestBase
{
    protected readonly ITestOutputHelper _testOutputHelper;
    protected readonly TestHelper _testHelper;
    protected readonly CustomDbContext _dbContext;
    protected readonly CustomUserStore _userStore;
    protected readonly CustomUserManager _userManager;
    protected readonly IPasswordHasher<CustomUser> _passwordHasher;
    protected readonly MemoryCacheService _memoryCacheService;


    protected TestBase(ITestOutputHelper testOutputHelper)
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
    }

    protected DefaultHttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse(TestHelper.IpAddress) }
        };
        httpContext.Request.Headers.UserAgent = TestHelper.UserAgent;
        httpContext.Request.Host = new HostString("localhost", 5000);
        return httpContext;
    }

    protected async Task<CustomUser> AddMockUserAsync()
    {
        var userMock = new CustomUser
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
        userMock.PasswordHash = _passwordHasher.HashPassword(userMock, "This%Valid_And#Pass%mock-2026");
        return await _userStore.AddUserAsync(userMock, true);
    }
}