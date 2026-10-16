using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/users")]
[Authorize] // All user endpoints require authentication
public class UsersController(AccountService service) : ControllerBase
{
    // GET: api/users/profile - Get user profile
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        return Ok(await service.GetProfile());
    }

    // PUT: api/users/profile - Update user profile
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        return Ok(await service.UpdateProfile(request));
    }

    // GET: api/users/profile/stats - Get user statistics
    [HttpGet("profile/stats")]
    public async Task<ActionResult<UserStatsResponse>> GetUserStats()
    {
        return Ok(await service.GetUserStats());
    }

    // GET: api/users/profile/avatar - Get user avatar URL
    [HttpGet("profile/avatar")]
    public async Task<ActionResult> GetAvatar()
    {
        return Ok(await service.GetAvatar());
    }

    // POST: api/users/profile/avatar - Update user avatar (set avatar URL)
    [HttpPost("profile/avatar")]
    public async Task<ActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
    {
        return Ok(await service.UpdateAvatar(request));
    }
}
