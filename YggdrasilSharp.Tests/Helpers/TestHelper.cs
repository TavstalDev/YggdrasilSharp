using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
    public static ServiceProvider ServiceProvider { get; private set; } = new ServiceCollection()
        .AddLogging()
        .AddMemoryCache()
        .BuildServiceProvider();

    public static MemoryCacheService MemoryCacheService { get; private set; } = new(ServiceProvider.GetRequiredService<IMemoryCache>());
    
    public static FakeEmailService FakeEmailService { get; private set; } = new();
    
    public static IPasswordHasher<CustomUser> PasswordHasher { get; private set; } = new PasswordHasher<CustomUser>();
    
    public static void InitTestServices()
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

    public static CustomUserManager CreateCustomUserManager(CustomDbContext db, CustomUserStore userStore)
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

    public static CustomSignInManager CreateSignInManager(CustomUserStore userStore, CustomUserManager userManager, Settings settings)
    {
        return new CustomSignInManager(userStore, userManager, PasswordHasher, MemoryCacheService, settings);
    }

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
            "MesterMC Development",
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
}