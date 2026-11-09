using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Web.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/staff")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;
        private readonly ICurrentUserService _currentUser;

        public StaffController(IStaffService staffService, ICurrentUserService currentUser)
        {
            _staffService = staffService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// აბრუნებს ყველა რეგისტრატორს (მხოლოდ ადმინი)
        /// </summary>
        [HttpGet("receptionists")]
        public async Task<IActionResult> GetReceptionists(CancellationToken cancellationToken)
        {
            var receptionists = await _staffService.GetStaffAsync(Roles.Receptionist, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(receptionists, "Receptionists retrieved successfully."));
        }

        /// <summary>
        /// ქმნის რეგისტრატორს ანგარიშით და "Receptionist" როლით (მხოლოდ ადმინი)
        /// </summary>
        [HttpPost("receptionists")]
        public async Task<IActionResult> CreateReceptionist([FromBody] CreateStaffDTO dto, CancellationToken cancellationToken)
        {
            var receptionist = await _staffService.AddStaffAsync(Roles.Receptionist, dto, cancellationToken);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<object>.Succeed(receptionist, "Receptionist created successfully."));
        }

        /// <summary>
        /// აბრუნებს ყველა ადმინისტრატორს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins(CancellationToken cancellationToken)
        {
            var admins = await _staffService.GetStaffAsync(Roles.Admin, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(admins, "Admins retrieved successfully."));
        }

        /// <summary>
        /// ქმნის ახალ ადმინისტრატორს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpPost("admins")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateStaffDTO dto, CancellationToken cancellationToken)
        {
            var admin = await _staffService.AddStaffAsync(Roles.Admin, dto, cancellationToken);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<object>.Succeed(admin, "Admin created successfully."));
        }

        /// <summary>
        /// ჩართავს ადმინის ან რეგისტრატორის ანგარიშს (ხსნის დაბლოკვას)
        /// </summary>
        /// <param name="userId">ანგარიშის id (userId სიიდან)</param>
        [HttpPost("{userId}/activate")]
        public async Task<IActionResult> Activate(string userId, CancellationToken cancellationToken)
        {
            await _staffService.SetStaffActiveAsync(
                userId,
                true,
                _currentUser.UserId ?? string.Empty,
                _currentUser.IsInRole(Roles.SuperAdmin),
                cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Account activated successfully."));
        }

        /// <summary>
        /// თიშავს ადმინის ან რეგისტრატორის ანგარიშს. საკუთარი თავისა და ბოლო აქტიური ადმინის გათიშვა აკრძალულია
        /// </summary>
        /// <param name="userId">ანგარიშის id (userId სიიდან)</param>
        [HttpPost("{userId}/deactivate")]
        public async Task<IActionResult> Deactivate(string userId, CancellationToken cancellationToken)
        {
            await _staffService.SetStaffActiveAsync(
                userId,
                false,
                _currentUser.UserId ?? string.Empty,
                _currentUser.IsInRole(Roles.SuperAdmin),
                cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Account deactivated successfully."));
        }
    }
}
