using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using Tavstal.YggdrasilSharp.Controllers.User;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Xunit;

namespace Tavstal.YggdrasilSharp.Tests.Controllers.User;

/// <summary>
/// Tests for <see cref="UserCapesController"/>.
/// </summary>
public class UserCapesControllerTests : ControllerTestBase
{
    private readonly IRepository<FileData> _fileDataRepo;
    private readonly IRepository<Cape> _capeRepo;
    private readonly Mock<ILogger<UserCapesController>> _loggerMock = new();
    private readonly UserCapesController _controller;
    
    /// <summary>
    /// Initializes a new instance of <see cref="UserCapesControllerTests"/>.
    /// Creates a controller with a mock logger, the test user manager, test database and settings, and assigns a test HttpContext.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper forwarded to base class for logging test output.</param>
    public UserCapesControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _fileDataRepo = new Repository<FileData>(_dbContext);
        _capeRepo = new Repository<Cape>(_dbContext);
        _controller = new UserCapesController(_loggerMock.Object, _userManager, _userStore, _settings);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _controllerHttpContext
        };
    }

    /// <summary>
    /// Tests for selecting a cape for the current authenticated user.
    /// Validates success, duplicate-selection and failure cases (not found / unauthorized).
    /// </summary>
    public class SelectSkinTests : UserCapesControllerTests
    {
        public SelectSkinTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Success case: an authenticated user selects an existing cape.
        /// </summary>
        [Fact(DisplayName = "Success: Select cape")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            var result = await _controller.SelectCape(db.cape.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: attempting to select a cape that is already selected by the user.
        /// </summary>
        [Fact(DisplayName = "Failure: Cape already selected")]
        public async Task ReturnsBadRequest()
        {
            var user = await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            await _controller.SelectCape(db.cape.Id);
            var result = await _controller.SelectCape(db.cape.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: selecting a non-existent cape id.
        /// </summary>
        [Fact(DisplayName = "Failure: Cape not found")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            var result = await _controller.SelectCape(1);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: user not authenticated when attempting to select a cape.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            var result = await _controller.SelectCape(1);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Tests for clearing the currently selected cape for the authenticated user.
    /// </summary>
    public class ClearSelectedSkinTests : UserCapesControllerTests
    {
        public ClearSelectedSkinTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: clears the currently selected cape for the authenticated user.
        /// </summary>
        [Fact(DisplayName = "Success: Clear selected cape")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            db.userCape.IsSelected = true;
            await _userStore.UserCapes.UpdateAsync(db.userCape, true, TestContext.Current.CancellationToken);
            var result = await _controller.ClearSelectedCape();
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: no cape is currently selected for the user.
        /// </summary>
        [Fact(DisplayName = "Failure: No cape selected")]
        public async Task ReturnsBadRequest()
        {
            var user =  await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            var result = await _controller.ClearSelectedCape();
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: unauthenticated request to clear selected cape.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            var result = await _controller.ClearSelectedCape();
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(401);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin tests for selecting a cape for another user via admin endpoint.
    /// </summary>
    public class SelectSkinAdminTests : UserCapesControllerTests
    {
        public SelectSkinAdminTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
        
        /// <summary>
        /// Success case: admin selects an existing cape for another user.
        /// </summary>
        [Fact(DisplayName = "Success: Select cape")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            var result = await _controller.SelectCapeAdmin(user.Id, db.cape.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: admin attempts to select a cape that is already selected for that user.
        /// </summary>
        [Fact(DisplayName = "Failure: Cape already selected")]
        public async Task ReturnsBadRequest()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            await _controller.SelectCapeAdmin(user.Id, db.cape.Id);
            var result = await _controller.SelectCapeAdmin(user.Id, db.cape.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(400);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: admin selects a non-existent cape id for the target user.
        /// </summary>
        [Fact(DisplayName = "Failure: Cape not found")]
        public async Task ReturnsNotFound()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            var result = await _controller.SelectCapeAdmin(user.Id, 1);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: caller does not have sufficient admin permissions to perform the action.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            var result = await _controller.SelectCapeAdmin(user.Id, 1);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }
    
    /// <summary>
    /// Admin tests for clearing the selected cape for another user via admin endpoint.
    /// </summary>
    public class ClearSelectedSkinAdminTests : UserCapesControllerTests
    {
        public ClearSelectedSkinAdminTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }
      
        /// <summary>
        /// Success case: admin clears the selected cape for another user.
        /// </summary>
        [Fact(DisplayName = "Success: Clear selected cape")]
        public async Task ReturnsOk()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            db.userCape.IsSelected = true;
            await _userStore.UserCapes.UpdateAsync(db.userCape, true, TestContext.Current.CancellationToken);
            var result = await _controller.ClearSelectedCapeAdmin(user.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(200);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: target user has no cape selected.
        /// </summary>
        [Fact(DisplayName = "Failure: No cape selected")]
        public async Task ReturnsBadRequest()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            var db = await FillDatabase(user.Id);
            var result = await _controller.ClearSelectedCapeAdmin(user.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(404);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
        
        /// <summary>
        /// Failure case: admin lacks sufficient permissions to clear another user's selected cape.
        /// </summary>
        [Fact(DisplayName = "Failure: Not enough permissions")]
        public async Task ReturnsForbidden()
        {
            var user =  await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller, givePermissions: false);
            var result = await _controller.ClearSelectedCapeAdmin(user.Id);
            
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
            _testOutputHelper.WriteLine("Result: " + objectResult.Value);
        }
    }

    /// <summary>
    /// Helper used by tests to create a file/cape and user-cape relation in the test database.
    /// It saves an in-memory generated image as FileData, creates a Cape referencing that file and associates it with the specified user.
    /// </summary>
    /// <param name="userId">Id of the user to whom the created cape/userCape should belong.</param>
    /// <returns>
    /// A tuple containing:
    /// <br/>- <see cref="FileData"/> representing the saved image file record,
    /// <br/>- <see cref="Cape"/> the created cape record,
    /// <br/>- <see cref="UserCape"/> the user-cape association.
    /// </returns>
    private async Task<(FileData fileData, Cape cape, UserCape userCape)> FillDatabase(string userId)
    {
        using var stream = CreateTestImage(64, 64);
        using var sha256 = SHA256.Create();
        byte[] hashBytes = await sha256.ComputeHashAsync(stream);
        string fileHash = Convert.ToHexStringLower(hashBytes);
            
        var fd = await _fileDataRepo.AddAsync(new FileData
        {
            Hash = fileHash,
            FileName = "skin.png",
            ContentType = "image/png",
            UserId = userId,
            Type = EFileDataType.CAPE,
        }, true);
        fd.SaveFile(stream);
        var cape = await _capeRepo.AddAsync(new Cape
        {
            Name = "Test Cape",
            FileId = fd.Id,
            IsPublic = true
        }, true);
        var userCape = await _userStore.UserCapes.AddAsync(new UserCape
        {
            UserId = userId,
            CapeId = cape.Id,
            IsSelected = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt =  DateTime.UtcNow
        }, true);
        return (fd, cape, userCape);
    }
    
    /// <summary>
    /// Utility: creates and returns an in-memory PNG image stream of the specified width and height.
    /// The returned <see cref="MemoryStream"/> is positioned at 0 and ready for reading.
    /// </summary>
    /// <param name="width">Width in pixels for the generated image.</param>
    /// <param name="height">Height in pixels for the generated image.</param>
    /// <returns>A <see cref="MemoryStream"/> containing a PNG image.</returns>
    private MemoryStream CreateTestImage(int width, int height)
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