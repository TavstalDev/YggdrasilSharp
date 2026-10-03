using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tavstal.YggdrasilSharp.Controllers.User;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Models.Database;
using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Services.Database;
using Tavstal.YggdrasilSharp.Services.Database.Interfaces;
using Tavstal.YggdrasilSharp.Tests.Helpers;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Tests.Controllers.User;

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
        _controller = new UserCapesController(_loggerMock.Object, _userManager, _userStore, _memoryCacheService, AppConfiguration);
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
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
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
            
            TestHelper.TestResponse(result, HttpStatusCode.OK);
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
            
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
        }
        
        /// <summary>
        /// Failure case: selecting a non-existent cape id.
        /// </summary>
        [Fact(DisplayName = "Failure: Cape not found")]
        public async Task ReturnsNotFound()
        {
            await CreateUserAsync(_controller);
            var result = await _controller.SelectCape(1);
            
            TestHelper.TestResponse(result, HttpStatusCode.NotFound);
        }
        
        /// <summary>
        /// Failure case: user not authenticated when attempting to select a cape.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            var result = await _controller.SelectCape(1);
            
            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized);
        }
    }
    
    /// <summary>
    /// Tests for clearing the currently selected cape for the authenticated user.
    /// </summary>
    public class ClearSelectedSkinTests : UserCapesControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
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
            
            TestHelper.TestResponse(result, HttpStatusCode.OK);
        }
        
        /// <summary>
        /// Failure case: no cape is currently selected for the user.
        /// </summary>
        [Fact(DisplayName = "Failure: No cape selected")]
        public async Task ReturnsBadRequest()
        {
            var user =  await CreateUserAsync(_controller);
            await FillDatabase(user.Id);
            var result = await _controller.ClearSelectedCape();
            
            TestHelper.TestResponse(result, HttpStatusCode.NotFound);
        }
        
        /// <summary>
        /// Failure case: unauthenticated request to clear selected cape.
        /// </summary>
        [Fact(DisplayName = "Failure: Unauthorized")]
        public async Task ReturnsUnauthorized()
        {
            var result = await _controller.ClearSelectedCape();
            
            TestHelper.TestResponse(result, HttpStatusCode.Unauthorized);
        }
    }
    
    /// <summary>
    /// Admin tests for selecting a cape for another user via admin endpoint.
    /// </summary>
    public class SelectSkinAdminTests : UserCapesControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
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
            
            TestHelper.TestResponse(result, HttpStatusCode.OK);
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
            
            TestHelper.TestResponse(result, HttpStatusCode.BadRequest);
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
            
            TestHelper.TestResponse(result, HttpStatusCode.NotFound);
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
            
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden);
        }
    }
    
    /// <summary>
    /// Admin tests for clearing the selected cape for another user via admin endpoint.
    /// </summary>
    public class ClearSelectedSkinAdminTests : UserCapesControllerTests
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="testOutputHelper">The output helper used to write test diagnostics.</param>
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
            
            TestHelper.TestResponse(result, HttpStatusCode.OK);
        }
        
        /// <summary>
        /// Failure case: target user has no cape selected.
        /// </summary>
        [Fact(DisplayName = "Failure: No cape selected")]
        public async Task ReturnsBadRequest()
        {
            var user = await CreateUserAsync(_controller, _userMock2, false);
            await CreateUserAsync(_controller);
            await FillDatabase(user.Id);
            var result = await _controller.ClearSelectedCapeAdmin(user.Id);
            
            TestHelper.TestResponse(result, HttpStatusCode.NotFound);
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
            
            TestHelper.TestResponse(result, HttpStatusCode.Forbidden);
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
    /// <br/>- <see cref="Cape"/> the created Hcape record,
    /// <br/>- <see cref="UserCape"/> the user-cape association.
    /// </returns>
    private async Task<(FileData fileData, Cape cape, UserCape userCape)> FillDatabase(string userId)
    {
        using var stream = TestHelper.CreateTestImage(64, 64);
        using var sha256 = SHA256.Create();
        byte[] hashBytes = await sha256.ComputeHashAsync(stream);
        string fileHash = Convert.ToHexStringLower(hashBytes);
            
        var fd = await _fileDataRepo.AddAsync(new FileData
        {
            Hash = fileHash,
            FileName = $"{Guid.NewGuid():N}.png",
            ContentType = "image/png",
            UserId = userId,
            Type = EFileDataType.CAPE,
        }, true);
        await fd.SaveFileAsync(stream);
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
}