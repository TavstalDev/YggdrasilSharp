using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkiaSharp;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Tests.Services;
using Tavstal.YggdrasilSharp.Utils.Helpers;

namespace Tavstal.YggdrasilSharp.Tests.Helpers;

public class TestHelper
{
    public const string UserAgent = "UnitTest/1.0";
    public const string IpAddress = "127.0.0.1";

    /// <summary>
    /// A fresh service provider for this helper instance. Never shared between tests so that
    /// parallel test classes cannot interfere with each other's caches or email records.
    /// </summary>
    public ServiceProvider ServiceProvider { get; }

    public MemoryCacheService MemoryCacheService { get; }

    public FakeEmailService FakeEmailService { get; }

    public IPasswordHasher<CustomUser> PasswordHasher { get; } = new PasswordHasher<CustomUser>();

    public TestHelper()
    {
        ServiceProvider = new ServiceCollection()
            .AddLogging()
            .AddMemoryCache()
            .BuildServiceProvider();

        MemoryCacheService = new MemoryCacheService(ServiceProvider.GetRequiredService<IMemoryCache>());
        FakeEmailService = new FakeEmailService();
    }

    public static string GetFingerprint(string userId)
    {
        var rawData = $"{userId}-{UserAgent}-{IpAddress}";
        return StringChiper.GetEncryptedHash(rawData, "QvHRAnkn2cr7fTa2PjcaWaQhKndzRNl6");
    }
    
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

    public CustomSignInManager CreateSignInManager(CustomUserStore userStore, CustomUserManager userManager, Settings settings) =>
        new(userStore, userManager, PasswordHasher, MemoryCacheService, settings);

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

    public static Settings CreateTestSettings()
    {
        var (pfxFilePath, password) = CreateSelfSignedPfxFile();
        
        return new Settings(
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
    
    public static (string filePath, string password) CreateSelfSignedPfxFile(string subjectName = "CN=localhost", string password = "changeit")
    {
        var (pfxBytes, _) = CreateSelfSignedPfx(subjectName, password);
        
        // Save to a temporary file with .pfx extension
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"test-cert-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(tempFilePath, pfxBytes);
        
        return (tempFilePath, password);
    }
    
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
}