using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Authentication;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Authentication;

public class BearerAuthTests : TestBase
{
    private readonly AuthenticationScheme _scheme;
    private readonly BearerAuthenticationHandler _handler;
    
    public BearerAuthTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
            
        var logger = new Mock<ILogger<BearerAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<String>())).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>();
        _scheme = new AuthenticationScheme("Bearer", null, typeof(BearerAuthenticationHandler));
        _handler = new BearerAuthenticationHandler(options.Object, loggerFactory.Object, encoder.Object, _userManager, _userStore);
    }
    
    [Fact(DisplayName = "Success: Authentication was completed.")]
    public async Task SuccessfulAuth()
    {
        var user = await AddMockUserAsync();
        var token = await _userStore.UserTokens.AddAsync(new CustomUserToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Name = "AccessToken",
            LoginProvider = "Default",
            Value = _userManager.CreateJwtToken(TimeSpan.FromDays(1)),
            CreateDate = DateTimeOffset.UtcNow
        }, true, TestContext.Current.CancellationToken);
        await _userStore.UserLogins.AddAsync(new CustomUserLogin
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
        
        var context = GetHttpContext();
        var authorizationHeader = new StringValues($"Bearer {token.Value}");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeTrue();
        result.None.Should().BeFalse();
        result.Failure.Should().BeNull();
    }
    
    [Fact(DisplayName = "Fail: The provided authentication header was empty.")]
    public async Task FailureEmptyAuthHeader()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues(string.Empty);
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }
    
    [Fact(DisplayName = "Fail: The provided authentication header was not Bearer.")]
    public async Task FailureHeaderNotBearer()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Invalid abcd");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }
    
    [Fact(DisplayName = "Fail: The provided Bearer header did not contain a token.")]
    public async Task FailureEmptyBearer()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Bearer");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Invalid authentication information provided in the request.");
    }
    
    [Fact(DisplayName = "Fail: The provided bearer token could not be verified.")]
    public async Task FailureTokenCouldNotBeVerified()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Bearer abcd");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Failed to authenticate user with provided authentication information.");
    }
}