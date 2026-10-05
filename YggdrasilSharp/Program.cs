using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using DotEnv.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Tavstal.YggdrasilSharp.Models;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.RateLimiting;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Services.Authentication;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using IPNetwork = System.Net.IPNetwork;
using RateLimits = Tavstal.YggdrasilSharp.Models.RateLimiting.Constants.RateLimits;

namespace Tavstal.YggdrasilSharp;

/// <summary>
/// Entry point for the API application.
///
/// This static class is responsible for:
/// <br/>- Bootstrapping the application with logging and configuration
/// <br/>- Orchestrating the setup of services, middleware, database, and Kestrel server
/// <br/>- Managing the application lifecycle from startup to runtime
/// <br/><br/>
/// Configuration flow:
/// <br/>1. Load environment variables from .env file
/// <br/>2. Configure Kestrel (HTTPS with SSL certificate)
/// <br/>3. Register all services (database, authentication, caching, rate limiting, etc.)
/// <br/>4. Build the application
/// <br/>5. Initialize the database
/// <br/>6. Configure middleware pipeline
/// <br/>7. Run the application
/// </summary>
public static class Program
{
    /// <summary>
    /// Logger instance for recording startup and runtime events.
    /// </summary>
    private static ILogger? _logger;

    private static AppConfiguration? _settings;

    /// <summary>
    /// Indicates whether the application is running in Development environment.
    /// </summary>
    internal static bool IsDevelopment { get; set; }

    /// <summary>
    /// Gets the content root path of the application (typically the project root directory).
    /// </summary>
    public static string ContentRoot { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the directory path where user-uploaded files are stored.
    /// </summary>
    internal static string UploadDir { get; set; } = string.Empty;

    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A task that represents the asynchronous lifetime of the running web application.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var bootstrapFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = bootstrapFactory.CreateLogger("Program");

            var builder = WebApplication.CreateBuilder(args);
            // 1. Load configuration
            ConfigureAppConfiguration(builder);
            // 2. Configure Kestrel
            ConfigureKestrel(builder);
            // 3. Add Services (Equivalent to Startup.ConfigureServices)
            ConfigureServices(builder);

            // 4. Build the app
            var app = builder.Build();

            // 5. Configure the database
            if (!await InitializeDatabaseAsync(app))
                return 1;

            // 6. Configure Middleware (Equivalent to Startup.Configure)
            ConfigureMiddleware(app);

            using var cts = new CancellationTokenSource();
            await app.RunAsync(cts.Token);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host terminated unexpectedly: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Configures the application configuration by loading environment variables from a .env file.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance used to configure the application.</param>
    private static void ConfigureAppConfiguration(WebApplicationBuilder builder)
    {
        ContentRoot = builder.Environment.ContentRootPath;
        IsDevelopment = builder.Environment.IsDevelopment();

        // Load your .env file
        new EnvLoader()
            .AddEnvFile(Path.Combine(ContentRoot, ".env"))
            .Load();

        // IMPORTANT: Re-add environment variables to pick up what EnvLoader just set
        builder.Configuration.AddEnvironmentVariables();

        UploadDir = Path.Combine(ContentRoot, builder.Configuration.GetValue<string>(Constants.ConfigurationKeys.RuntimeUploadDir) ?? "wwwuploads");
        if (!Directory.Exists(UploadDir))
            Directory.CreateDirectory(UploadDir);
    }

    /// <summary>
    /// Registers and configures all application services including:
    /// <br/>- Database context and repositories
    /// <br/>- Authentication (JWT Bearer, Basic)
    /// <br/>- Authorization and identity services
    /// <br/>- Swagger/OpenAPI documentation
    /// <br/>- CORS policies and session management
    /// <br/>- Rate limiting and caching
    /// <br/>- Hosted services (database cleanup, email service)
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance for registering services.</param>
    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        _settings = new AppConfiguration(configuration);

        #region Database
        // Configure identity options
        services.Configure<IdentityOptions>(x =>
        {
            x.User.AllowedUserNameCharacters = _settings.AllowedUsernameCharacters;
        });

        string connectionString = _settings.Database.ConnectionString;
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is missing from the configuration.");

        connectionString = connectionString
            .Replace($"${Constants.EnvironmentKeys.DatabaseUser}", configuration.GetValue<string>(Constants.EnvironmentKeys.DatabaseUser))
            .Replace($"${Constants.EnvironmentKeys.DatabasePassword}", configuration.GetValue<string>(Constants.EnvironmentKeys.DatabasePassword));

        string databaseProvider = _settings.Database.Provider;
        if (string.IsNullOrEmpty(databaseProvider))
            throw new InvalidOperationException("Database Provider is missing from the configuration.");


        string? databaseVersion = configuration.GetValue<string>(Constants.ConfigurationKeys.DatabaseVersion);
        if (string.IsNullOrEmpty(databaseVersion) || !Version.TryParse(databaseVersion, out Version? dbVersion))
            throw new  InvalidOperationException("Database Version is missing from the configuration.");

        // Configure the database context
        services.AddDbContext<CustomDbContext>(options =>
        {
            switch (databaseProvider.ToLower())
            {
                case "postgresql":
                    options.UseNpgsql(connectionString, optionsBuilder =>
                        optionsBuilder.EnableRetryOnFailure());
                    break;
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                default:
                    options.UseMySql(connectionString, new MySqlServerVersion(dbVersion), optionsBuilder => optionsBuilder.EnableRetryOnFailure());
                    break;
            }
        });
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        #endregion
        #region Swagger Docs
        // Configure JSON options for controllers
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        // Configure Swagger generation
        services.AddSwaggerGen(config =>
        {
            // Add Swagger document with version and metadata
            config.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = _settings.Swagger.Name,
                Version = _settings.Swagger.Version,
                Description = _settings.Swagger.Description,
                Contact = new OpenApiContact
                {
                    Name = _settings.Swagger.ContactName,
                    Url = new Uri(_settings.Swagger.ContactLink)
                },
                License = new OpenApiLicense
                {
                    Name = _settings.Swagger.LicenseName,
                    Url = new Uri(_settings.Swagger.LicenseLink)
                }
            });
            // Include XML comments for better documentation
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            config.IncludeXmlComments(xmlPath);
            // Enable annotations for Swagger
            config.EnableAnnotations();
            // Add security definition for Bearer token
            config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\nExample: \"Bearer 12345abcdef\"",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer",
            });
            // Add security definition for Basic authentication
            config.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Basic scheme. \r\n\r\nExample: \"Basic 12345abcdef:12345abcdef\"",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "basic",
            });
            // Add security requirements for Bearer and Basic authentication
            config.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer", doc), [] },
                { new OpenApiSecuritySchemeReference("Basic", doc), [] }
            });
        });
        #endregion
        #region Authentication
        services.AddScoped<CustomUserStore>();
        services.AddScoped<CustomUserManager>();
        services.AddScoped<CustomSignInManager>();
        services.AddScoped<IPasswordHasher<CustomUser>, PasswordHasher<CustomUser>>();

        services.AddHttpContextAccessor();
        // Configure authentication schemes
        services.AddAuthentication(x =>
            {
                // Set the default challenge scheme to JWT Bearer
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                // Set the default authentication scheme to JWT Bearer
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                // Set the default sign-in scheme to JWT Bearer
                x.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                // Set the default sign-out scheme to JWT Bearer
                x.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Add JWT Bearer authentication  scheme
            .AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>("Bearer", null)
            // Add Basic authentication scheme
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);
        #endregion

        // Configure form options
        int bodyLengthLimit = configuration.GetValue(Constants.ConfigurationKeys.RateLimitingUploadLimit, 100);
        services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 1024 * 1024 * bodyLengthLimit; });

        #region Cors
        // Retrieve the CORS configuration section from the application configuration
        var corsDefault = configuration.GetSection("CORS:Default");
        // Add distributed memory cache services

        var allowAnyOrigin = corsDefault.GetValue<bool>("AllowAnyOrigin");
        var allowAnyMethod = corsDefault.GetValue<bool>("AllowAnyMethod");
        var allowAnyHeader = corsDefault.GetValue<bool>("AllowAnyHeader");
        if (allowAnyOrigin && allowAnyHeader)
            _logger?.LogWarning("CORS configuration allows any origin and any header, which may have security implications. Ensure this is intentional.");

        int idleTimeout = configuration.GetValue(Constants.ConfigurationKeys.CorsIdleTimeout, 300);
        services.AddDistributedMemoryCache()
            // Add session services with specified options
            .AddSession(options =>
            {
                // Set the session idle timeout
                options.IdleTimeout = TimeSpan.FromSeconds(idleTimeout);
                // Ensure the session cookie is HTTP-only
                options.Cookie.HttpOnly = true;
                // Mark the session cookie as essential
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            })
            // Add CORS services with specified options
            .AddCors(options =>
            {
                // Add a CORS policy named "Default"
                options.AddPolicy(name: "Default",
                    policy =>
                    {
                        // Allow origins specified in the CORS configuration section
                        policy.WithOrigins(corsDefault.GetSection("Sites").Get<string[]>() ?? throw new Exception("Cors sites section is not set"));
                        policy.WithHeaders(corsDefault.GetSection("Headers").Get<string[]>() ?? []);
                        policy.WithMethods(corsDefault.GetSection("Methods").Get<string[]>() ?? []);
                        policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsDefault.GetValue("MaxAge", 3600)));
                        // Allow any origin if specified in the CORS configuration
                        if (allowAnyOrigin)
                            policy.AllowAnyOrigin();
                        // Allow any header if specified in the CORS configuration
                        if (allowAnyHeader)
                            policy.AllowAnyHeader();
                        // Allow any method if specified in the CORS configuration
                        if (allowAnyMethod)
                            policy.AllowAnyMethod();
                    });
            });
        #endregion
        #region Services
        // Configure identity options for claims
        services.Configure<IdentityOptions>(options => options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier);
        // Add HTTP client factory for making HTTP requests
        services.AddHttpClient();
        services.AddProblemDetails();
        var healthChecks = services.AddHealthChecks();
        healthChecks.AddDbContextCheck<CustomDbContext>(
            name: "database",
            tags: ["db", "ready"]
        );
        // Add memory caching services
        services.AddMemoryCache();
        services.AddSingleton<MemoryCacheService>();
        // Configure IP rate limiting
        #region Rate Limiting

        var rules = configuration
            .GetSection(Constants.ConfigurationKeys.RateLimitingRules)
            .Get<RateLimiterData>();
        if (rules == null)
            throw new InvalidOperationException("Rate limiting rules are missing from the configuration.");
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = configuration.GetValue(Constants.ConfigurationKeys.RateLimitingStatusCode, 429);

            // Partitions every policy by the authenticated user, falling back to the client IP
            // address. The Add*Limiter overloads register a single global limiter, so a single
            // noisy actor would otherwise consume the whole endpoint budget for every user.
            // RemoteIpAddress is resolved through UseForwardedHeaders, so this is the real client
            // address when running behind a configured trusted proxy.
            static string GetPartitionKey(HttpContext httpContext)
            {
                string? userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                    return $"user:{userId}";
                return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
            }

            // Fixed Window
            foreach (var ruleEntry in rules.FixedWindow)
            {
                var rule = ruleEntry.Value;
                options.AddPolicy(ruleEntry.Key, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(httpContext), _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rule.PermitLimit,
                        Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                        QueueLimit = rule.QueueLimit,
                        QueueProcessingOrder = rule.ProcessingOrder,
                    }));
            }

            // Sliding Window
            foreach (var ruleEntry in rules.SlidingWindow)
            {
                var rule = ruleEntry.Value;
                options.AddPolicy(ruleEntry.Key, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(GetPartitionKey(httpContext), _ => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rule.PermitLimit,
                        Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                        QueueLimit = rule.QueueLimit,
                        QueueProcessingOrder = rule.ProcessingOrder,
                        SegmentsPerWindow = rule.SegmentsPerWindow,
                    }));
            }

            // Concurrent
            foreach (var ruleEntry in rules.Concurrent)
            {
                var rule = ruleEntry.Value;
                options.AddPolicy(ruleEntry.Key, httpContext =>
                    RateLimitPartition.GetConcurrencyLimiter(GetPartitionKey(httpContext), _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = rule.PermitLimit,
                        QueueLimit = rule.QueueLimit,
                        QueueProcessingOrder = rule.ProcessingOrder,
                    }));
            }

            // Token Bucket
            foreach (var ruleEntry in rules.TokenBucket)
            {
                var rule = ruleEntry.Value;
                options.AddPolicy(ruleEntry.Key, httpContext =>
                    RateLimitPartition.GetTokenBucketLimiter(GetPartitionKey(httpContext), _ => new TokenBucketRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        QueueLimit = rule.QueueLimit,
                        QueueProcessingOrder = rule.ProcessingOrder,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(rule.ReplenishmentSeconds),
                        TokenLimit = rule.TokenLimit,
                        TokensPerPeriod = rule.TokensPerPeriod,
                    }));
            }
        });
        #endregion
        // Database cleaner service
        services.AddHostedService<DatabaseCleanerService>();
        // JwtSettings
        services.AddSingleton<AppConfiguration>();
        // Email Service
        services.AddSingleton<IEmailService, EmailService>();
        #endregion
    }

    /// <summary>
    /// Configures the Kestrel web server, including HTTPS setup with an SSL certificate.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance used to configure Kestrel.</param>
    private static void ConfigureKestrel(WebApplicationBuilder builder)
    {
        // At this point, builder.Configuration is fully populated
        var thumbprint = builder.Configuration[Constants.EnvironmentKeys.CertificateFingerprint];
        var password = builder.Configuration.GetValue<string>(Constants.EnvironmentKeys.CertificatePassword);
        var port = builder.Configuration.GetValue(Constants.ConfigurationKeys.ApplicationPort, 5001);

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            try
            {
                X509Certificate2? certificate = null;

                if (!string.IsNullOrEmpty(thumbprint))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        certificate = GetCertificateFromStore(thumbprint);
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                             RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        // On Unix/Linux, treat thumbprint as a file path to the certificate
                        certificate = GetCertificateFromFile(thumbprint, password!);

                    if (certificate == null)
                        throw new Exception("Failed to load certificate.");
                }
                else
                    _logger?.LogWarning("No certificate configured. Server will listen on HTTP only.");

                serverOptions.ListenAnyIP(port, listenOptions =>
                {
                    if (certificate != null)
                        listenOptions.UseHttps(certificate);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to configure HTTPS certificate. Server will listen on HTTP only.");
            }
        });
    }


    /// <summary>
    /// Configures the ASP.NET Core middleware pipeline.
    /// </summary>
    /// <param name="app">The WebApplication instance to configure.</param>
    private static void ConfigureMiddleware(WebApplication app)
    {
        if (_settings == null)
        {
            _logger?.LogCritical("The configuration was not ready at middleware setup.");
            return;
        }

        // 1. EXCEPTION HANDLING
        if (IsDevelopment)
            app.UseDeveloperExceptionPage();

        // 2. FORWARDED HEADERS
        if (_settings.Proxy.Enabled)
        {
            ForwardedHeadersOptions options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                ForwardLimit = _settings.Proxy.ForwardLimit,
                RequireHeaderSymmetry = false
            };

            foreach (string proxy in _settings.Proxy.KnownProxies)
                if (IPAddress.TryParse(proxy, out IPAddress? parsed))
                    options.KnownProxies.Add(parsed);

            foreach (string network in _settings.Proxy.KnownNetworks)
                if (IPNetwork.TryParse(network, out IPNetwork parsedNetwork))
                    options.KnownIPNetworks.Add(parsedNetwork);

            foreach (string host in _settings.Proxy.AllowedHosts)
                options.AllowedHosts.Add(host);

            // Loopback is a safe default for local reverse proxies; do NOT add 0.0.0.0/0 here.
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);

            app.UseForwardedHeaders(options);
        }

        // 3. CORRELATION ID / LOGGING SCOPE
        app.Use(async (context, next) =>
        {
            // Access the current standard Trace ID
            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

            // Attach to response headers so clients can reference it
            context.Response.Headers["X-Correlation-ID"] = traceId;

            // Push into Serilog/NLog logging scope
            using (_logger?.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = traceId }))
            {
                await next();
            }
        });

        // 4. SECURITY HEADERS & REDIRECTION
        if (!IsDevelopment)
            app.UseHsts();

        app.UseHttpsRedirection();

        // Custom Security Headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; object-src 'none';");
            await next();
        });

        // 5. STATIC FILES & SWAGGER
        app.UseStaticFiles();

        if (IsDevelopment)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{_settings.Swagger.Name} v1");
                c.RoutePrefix = "docs";
            });
        }

        // 6. ROUTING
        app.UseRouting();

        // 7. CORS
        app.UseCors("Default");

        // 8. AUTHENTICATION, SESSION & AUTHORIZATION
        app.UseAuthentication();
        app.UseAuthorization();
        //app.UseSession();

        // 9. RATE LIMITING & ENDPOINTS
        app.UseRateLimiter();
        app.MapHealthChecks("/health");
        app.MapControllers().RequireRateLimiting(RateLimits.DEFAULT);
    }

    /// <summary>
    /// Initializes the application's database.
    /// </summary>
    /// <param name="app">The WebApplication instance containing the service provider.</param>
    private static async Task<bool> InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var configuration =  services.GetRequiredService<IConfiguration>();
            var database = services.GetRequiredService<CustomDbContext>();
            var userStore = services.GetRequiredService<CustomUserStore>();
            var passwordHasher = services.GetRequiredService<IPasswordHasher<CustomUser>>();
            await DatabaseInitializer.InitializeAsync(database, userStore, passwordHasher, configuration);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "An error occurred while initializing the database.");
            return false;
        }
    }

    /// <summary>
    /// Retrieves an X509 certificate based on the operating system platform.
    /// </summary>
    /// <param name="thumbprint">
    /// On Windows: The thumbprint (SHA-1 hash) of the certificate in the store (with or without colons, e.g., "9C:D2:8A:1B:7F:3E").
    /// <br/>On Unix/Linux/macOS: The file path to the certificate file (PEM or PKCS#12 format).
    /// </param>
    /// <param name="password">The password for the certificate file (required for encrypted PKCS#12 files on Unix/Linux systems).</param>
    /// <returns>The X509Certificate2 object containing the loaded certificate.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the operating system is not Windows, Linux, or macOS.</exception>
    /// <exception cref="Exception">Thrown when the certificate cannot be found or loaded (specific exceptions depend on the platform).</exception>
    public static X509Certificate2 GetCertificate(string thumbprint, string password)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetCertificateFromStore(thumbprint);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetCertificateFromFile(thumbprint, password);

        throw new PlatformNotSupportedException("Unsupported operating system platform.");
    }

    /// <summary>
    /// Retrieves an X509 certificate from the current user's certificate store using its thumbprint.
    /// </summary>
    /// <param name="thumbprint">
    /// The thumbprint (SHA-1 hash) of the certificate to retrieve in hexadecimal format.
    /// Can be with or without colons as separators (e.g., "9C:D2:8A:1B:7F:3E" or "9CD28A1B7F3E").
    /// </param>
    /// <returns>The X509Certificate2 object matching the provided thumbprint.</returns>
    /// <exception cref="Exception">Thrown when no certificate with the specified thumbprint is found in the certificate store. </exception>
    private static X509Certificate2 GetCertificateFromStore(string thumbprint)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

        if (certs.Count == 0)
            throw new Exception("Certificate not found!");

        return certs[0];
    }

    /// <summary>
    /// Retrieves an X509 certificate from a file on the file system (Unix/Linux/macOS compatible).
    /// </summary>
    /// <param name="filePath">
    /// The absolute file path to the certificate file.
    /// </param>
    /// <param name="password">The password for the certificate file.</param>
    /// <returns>The X509Certificate2 object loaded from the certificate file, including the private key if available.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the certificate file does not exist at the specified file path.</exception>
    private static X509Certificate2 GetCertificateFromFile(string filePath, string password)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Certificate file not found.", filePath);

        return X509CertificateLoader.LoadPkcs12FromFile(filePath, password);
    }
}
