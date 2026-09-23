using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Authentication;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Authentication;

public class CookieAuthTests : TestBase
{
    private readonly AuthenticationScheme _scheme;
    private readonly CookieAuthenticationHandler _handler;
    
    public CookieAuthTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
            
        var logger = new Mock<ILogger<CookieAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<String>())).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>();
        _scheme = new AuthenticationScheme("Cookie", null, typeof(CookieAuthenticationHandler));
        _handler = new CookieAuthenticationHandler(options.Object, loggerFactory.Object, encoder.Object, _userManager, _userStore);
    }
    
    [Fact(DisplayName = "Success: Authentication was completed.")]
    public async Task SuccessfulAuth()
    {
        var user = await AddMockUserAsync();
        var context = GetHttpContext();
        var token = await _userStore.UserTokens.AddAsync(new CustomUserToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Name = "AccessToken",
            LoginProvider = "Default",
            Value = _userManager.CreateJwtToken(TimeSpan.FromDays(1)),
            CreateDate = DateTimeOffset.UtcNow
        }, true, TestContext.Current.CancellationToken);
        var login = await _userStore.UserLogins.AddAsync(new CustomUserLogin
        {
            Id = 1,
            UserId = user.Id,
            ProviderDisplayName = "Default",
            LoginProvider = "Default",
            ProviderKey = token.Id,
            OperatingSystem = "Windows",
            IPv4Address = "127.0.0.1",
            CreateDate = DateTime.UtcNow,
            ExpireDate =  DateTime.UtcNow.AddDays(1)
        }, true, TestContext.Current.CancellationToken);
        
        
        context.Request.Headers.Append("Cookie", $"auth-token={token.Value}");
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.None.Should().BeFalse();
        result.Failure.Should().BeNull();
    }
    
    [Fact(DisplayName = "Fail: No cookie was provided.")]
    public async Task FailureEmptyAuthHeader()
    {
        var context = GetHttpContext();
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }
    
    [Fact(DisplayName = "Fail: The provided cookie was empty.")]
    public async Task FailureHeaderNotBasic()
    {
        var context = GetHttpContext();
        context.Request.Headers.Append("Cookie", $"auth-token=");
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }
    
    [Fact(DisplayName = "Fail: The provided cookie could not be verified.")]
    public async Task FailureCouldNotBeVerified()
    {
        var context = GetHttpContext();
        context.Request.Headers.Append("Cookie", $"auth-token=abcd");
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Failed to authenticate user with provided authentication information.");
    }
}