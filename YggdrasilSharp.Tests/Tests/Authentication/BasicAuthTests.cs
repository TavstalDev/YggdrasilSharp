using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Tavstal.YggdrasilSharp.Services.Authentication;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Authentication;

/// <summary>
/// Unit tests for <see cref="BasicAuthenticationHandler"/> covering successful authentication
/// and every malformed or unverifiable <c>Authorization</c> header it must reject.
/// </summary>
public class BasicAuthTests : TestBase
{
    private readonly AuthenticationScheme _scheme;
    private readonly BasicAuthenticationHandler _handler;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicAuthTests"/> class with a mocked options monitor,
    /// logger factory and URL encoder.
    /// </summary>
    /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
    public BasicAuthTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
            
        var logger = new Mock<ILogger<BasicAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<String>())).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>();
        _scheme = new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler));
        _handler = new BasicAuthenticationHandler(options.Object, loggerFactory.Object, encoder.Object, _userManager, _userStore);
    }
    
    /// <summary>
    /// Success: valid base64 encoded credentials of an existing user complete the authentication.
    /// </summary>
    [Fact(DisplayName = "Success: Authentication was completed.")]
    public async Task SuccessfulAuth()
    {
        var user = await AddMockUserAsync();
        var context = GetHttpContext();
        string basicValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:This%Valid_And#Pass%mock-2026"));
        var authorizationHeader = new StringValues($"Basic {basicValue}");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeTrue();
        result.None.Should().BeFalse();
        result.Failure.Should().BeNull();
    }
    
    /// <summary>
    /// Failure: an empty authorization header results in no authentication result.
    /// </summary>
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
    
    /// <summary>
    /// Failure: a header using another scheme results in no authentication result.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided authentication header was not Basic.")]
    public async Task FailureHeaderNotBasic()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Invalid abcd");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }
    
    /// <summary>
    /// Failure: a Basic header without credentials fails with an invalid header error.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided Basic header did not contain a token.")]
    public async Task FailureEmptyBasic()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Basic");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Invalid authentication header.");
    }
    
    /// <summary>
    /// Failure: credentials that are not base64 encoded fail with an encoding error.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided basic credentials were not encoded.")]
    public async Task FailureNotEncoded()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues("Basic abcd:efgh");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("The header parameter is not encoded correctly.");
    }
    
    /// <summary>
    /// Failure: well encoded credentials of an unknown user fail the authentication.
    /// </summary>
    [Fact(DisplayName = "Fail: The provided basic credentials could not be verified.")]
    public async Task FailureCouldNotBeVerified()
    {
        var context = GetHttpContext();
        var authorizationHeader = new StringValues($"Basic {Convert.ToBase64String("abcd:efgh"u8.ToArray())}");
        context.Request.Headers.Append(HeaderNames.Authorization, authorizationHeader);
        await _handler.InitializeAsync(_scheme, context);
        var result = await _handler.AuthenticateAsync();
        
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure.Message.Should().Be("Failed to authenticate user with provided authentication information.");
    }
}