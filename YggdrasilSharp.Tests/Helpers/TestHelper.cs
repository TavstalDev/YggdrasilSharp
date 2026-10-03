using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using SkiaSharp;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Responses;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Services;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Helpers;

/// <summary>
/// Shared test utilities: fake services, in-memory database factories, certificate generation
/// and response assertions used by the unit tests.
/// </summary>
public class TestHelper
{
    /// <summary>
    /// The user agent header value sent by every test request.
    /// </summary>
    public const string UserAgent = "UnitTest/1.0";

    /// <summary>
    /// The loopback IP address used as the remote address of every test request.
    /// </summary>
    public const string IpAddress = "127.0.0.1";

    /// <summary>
    /// A fresh service provider for this helper instance. Never shared between tests so that
    /// parallel test classes cannot interfere with each other's caches or email records.
    /// </summary>
    public ServiceProvider ServiceProvider { get; }

    /// <summary>
    /// The in-memory cache service backing the helper. Each instance owns its own cache, so cache
    /// entries written by one test are never visible to another test.
    /// </summary>
    public MemoryCacheService MemoryCacheService { get; }

    /// <summary>
    /// The fake email service capturing the emails produced by a test instead of sending them.
    /// </summary>
    public FakeEmailService FakeEmailService { get; }

    /// <summary>
    /// The password hasher used to create and verify password hashes inside tests.
    /// </summary>
    public IPasswordHasher<CustomUser> PasswordHasher { get; } = new PasswordHasher<CustomUser>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TestHelper"/> class with a dedicated service
    /// provider, memory cache service and fake email service.
    /// </summary>
    public TestHelper()
    {
        ServiceProvider = new ServiceCollection()
            .AddLogging()
            .AddMemoryCache()
            .BuildServiceProvider();

        MemoryCacheService = new MemoryCacheService(ServiceProvider.GetRequiredService<IMemoryCache>());
        FakeEmailService = new FakeEmailService();
    }

    /// <summary>
    /// Computes the device fingerprint the server derives for a user, using the same
    /// <see cref="UserAgent"/> and <see cref="IpAddress"/> values the tests send.
    /// </summary>
    /// <param name="userId">The identifier of the user the fingerprint belongs to.</param>
    /// <returns>The encrypted fingerprint of the user.</returns>
    public static string GetFingerprint(string userId)
    {
        var rawData = $"{userId}-{UserAgent}-{IpAddress}";
        return StringChiper.GetEncryptedHash(rawData, "QvHRAnkn2cr7fTa2PjcaWaQhKndzRNl6"u8.ToArray());
    }
    
    /// <summary>
    /// Creates a <see cref="CustomDbContext"/> backed by the EF in-memory provider.
    /// </summary>
    /// <param name="dbName">The name of the in-memory database. A new name is generated when omitted.</param>
    /// <returns>A database context with all tables created.</returns>
    public static CustomDbContext CreateInMemoryDbContext(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<CustomDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // CustomDbContext now only requires DbContextOptions in ctor
        var db = new CustomDbContext(options);

        // Ensure database tables created for EF InMemory
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>
    /// Creates a <see cref="CustomUserManager"/> wired to the given store and database context,
    /// using mocked HTTP and test settings instead of real infrastructure.
    /// </summary>
    /// <param name="db">The database context holding the user data.</param>
    /// <param name="userStore">The user store the manager operates on.</param>
    /// <returns>A user manager ready to be injected into a controller.</returns>
    public CustomUserManager CreateCustomUserManager(CustomDbContext db, CustomUserStore userStore)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>().Object;
        var logger = NullLogger<CustomUserManager>.Instance;

        var settings = CreateTestSettings();

        // New CustomUserManager signature: (CustomUserStore, IPasswordHasher<CustomUser>, IHttpClientFactory, ILogger<CustomUserManager>, CustomDbContext, MemoryCacheService, Settings)
        var manager = new CustomUserManager(
            userStore,
            PasswordHasher,
            httpClientFactory,
            logger,
            db,
            MemoryCacheService,
            settings
        );

        return manager;
    }

    /// <summary>
    /// Creates a <see cref="CustomSignInManager"/> wired to the given store, user manager and configuration.
    /// </summary>
    /// <param name="userStore">The user store the sign-in manager operates on.</param>
    /// <param name="userManager">The user manager the sign-in manager operates on.</param>
    /// <param name="appConfiguration">The application configuration used by the sign-in manager.</param>
    /// <returns>A sign-in manager ready to be injected into a controller.</returns>
    public CustomSignInManager CreateSignInManager(CustomUserStore userStore, CustomUserManager userManager, AppConfiguration appConfiguration) =>
        new(userStore, userManager, PasswordHasher, MemoryCacheService, appConfiguration);

    /// <summary>
    /// Create a <see cref="CustomUserStore"/> backed by the provided <see cref="CustomDbContext"/>.
    /// Tests can use this when they need to pass a <see cref="CustomUserStore"/> into controller constructors
    /// or to exercise store-specific behaviour.
    /// </summary>
    public static CustomUserStore CreateCustomUserStore(CustomDbContext db)
    {
        var usersRepo = new Repository<CustomUser>(db);
        var userClaimsRepo = new Repository<CustomUserClaim>(db);
        var userRolesRepo = new Repository<CustomUserRole>(db);
        var rolesRepo = new Repository<CustomRole>(db);
        var userTokensRepo = new Repository<CustomUserToken>(db);
        var userLoginsRepo = new Repository<CustomUserLogin>(db);
        var roleClaimsRepo = new Repository<IdentityRoleClaim<string>>(db);
        var userBackupCodesRepo = new Repository<UserBackupCode>(db);
        var userBillingRepo = new Repository<UserBillingInformation>(db);
        var userPlaySessionsRepo = new Repository<UserPlaySession>(db);
        var userCapesRepo = new Repository<UserCape>(db);

        return new CustomUserStore(
            usersRepo,
            userClaimsRepo,
            userRolesRepo,
            rolesRepo,
            userTokensRepo,
            userLoginsRepo,
            roleClaimsRepo,
            userBackupCodesRepo,
            userBillingRepo,
            userPlaySessionsRepo,
            userCapesRepo
        );
    }

    /// <summary>
    /// Builds an <see cref="AppConfiguration"/> filled with test-only values, including a freshly
    /// generated self-signed certificate for the HTTPS settings.
    /// </summary>
    /// <returns>A configuration instance usable by controllers and services under test.</returns>
    public static AppConfiguration CreateTestSettings()
    {
        var (pfxFilePath, password) = CreateSelfSignedPfxFile();
        
        return new AppConfiguration(
            "http://localhost",
            "http://localhost",
            "QvHRAnkn2cr7fTa2PjcaWaQhKndzRNl6",
            "test-issuer",
            "test-audience",
            TimeSpan.FromSeconds(5),
            5,
            TimeSpan.FromMinutes(15),
            "localhost",
            1025,
            "example@localhost",
            "12345678",
            ["localhost"],
            pfxFilePath,
            password,
            "Development",
            "yggdrasil-mock-server",
            "1.0.0"
            );
    }
    
    /// <summary>
    /// Generates a self-signed certificate and writes it to a temporary PFX file.
    /// </summary>
    /// <param name="subjectName">The distinguished name of the certificate subject.</param>
    /// <param name="password">The password protecting the exported PFX file.</param>
    /// <returns>The path of the generated PFX file and the password protecting it.</returns>
    public static (string filePath, string password) CreateSelfSignedPfxFile(string subjectName = "CN=localhost", string password = "changeit")
    {
        var (pfxBytes, _) = CreateSelfSignedPfx(subjectName, password);
        
        // Save to a temporary file with .pfx extension
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"test-cert-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(tempFilePath, pfxBytes);
        
        return (tempFilePath, password);
    }
    
    /// <summary>
    /// Generates a self-signed certificate valid for server authentication and exports it in PFX format.
    /// </summary>
    /// <param name="subjectName">The distinguished name of the certificate subject.</param>
    /// <param name="password">The password protecting the exported PFX bytes.</param>
    /// <returns>The exported PFX bytes, including the private key, and the password protecting them.</returns>
    public static (byte[] pfxBytes, string password) CreateSelfSignedPfx(string subjectName = "CN=localhost", string password = "changeit")
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // ServerAuth

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddYears(10);
        using var cert = req.CreateSelfSigned(notBefore, notAfter);

        // Export PFX bytes (include private key)
        var pfx = cert.Export(X509ContentType.Pkcs12, password);
        return (pfx, password);
    }
    
    /// <summary>
    /// Utility: creates and returns an in-memory PNG image stream of the specified width and height.
    /// The returned <see cref="MemoryStream"/> is positioned at 0 and ready for reading.
    /// </summary>
    /// <param name="width">Width in pixels for the generated image.</param>
    /// <param name="height">Height in pixels for the generated image.</param>
    /// <returns>A <see cref="MemoryStream"/> containing a PNG image.</returns>
    public static MemoryStream CreateTestImage(int width, int height)
    {
        var stream = new MemoryStream();
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
        stream.Position = 0; // Reset for reading
        return stream;
    }

    /// <summary>
    /// Asserts that a controller result is a <see cref="ContentResult"/> carrying an
    /// <see cref="ErrorResponse"/> payload with the expected status code and, optionally, message.
    /// </summary>
    /// <param name="result">The action result returned by the controller under test.</param>
    /// <param name="statusCode">The status code expected in the error response.</param>
    /// <param name="message">The message expected in the error response, or <c>null</c> to skip the message check.</param>
    /// <param name="testOutputHelper">Optional test output helper to log the response content for debugging.</param>
    public static void TestResponse(IActionResult result, HttpStatusCode statusCode, string? message = null, ITestOutputHelper? testOutputHelper = null)
    {
        result.Should().BeOfType<ContentResult>();
        var contentResult = result as ContentResult;
        contentResult.Should().NotBeNull();
        contentResult.Content.Should().NotBeNullOrEmpty();
        var response = JsonConvert.DeserializeObject<ErrorResponse>(contentResult.Content);
        testOutputHelper?.WriteLine($"Response content: {contentResult.Content}");
        response.Should().NotBeNull();
        testOutputHelper?.WriteLine($"Response status code: {response.StatusCode}\nResponse message: {response.Message}");
        response.StatusCode.Should().Be(statusCode);
        if (message != null)
            response.Message.Should().Be(message);
    }
}