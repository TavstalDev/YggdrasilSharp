using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Models;

/// <summary>
/// Base class for tests that only need an in-memory database, a user store, a user manager and a
/// request context, without a controller under test.
/// </summary>
public abstract class TestBase
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
    /// The memory cache service scoped to the test instance.
    /// </summary>
    protected readonly MemoryCacheService _memoryCacheService;

    /// <summary>
    /// The configuration shared by the services of this test instance. Token hashing performed by the
    /// code under test uses <c>AppConfiguration.Jwt.EncryptionKey</c> of this very instance.
    /// </summary>
    protected AppConfiguration AppConfiguration => _testHelper.Settings;


    /// <summary>
    /// Initializes a new instance of the <see cref="TestBase"/> class with fresh, non-shared test services.
    /// </summary>
    /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
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

    /// <summary>
    /// Creates a request context using the same user agent and loopback address as the other tests.
    /// </summary>
    /// <returns>A <see cref="DefaultHttpContext"/> ready to be passed to an authentication handler.</returns>
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

    /// <summary>
    /// Creates a confirmed user with a known password and stores it in the in-memory database.
    /// </summary>
    /// <returns>The created and persisted user.</returns>
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