using System.Security.Claims;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Web.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUser;

        public AuthController(IAuthService authService, ICurrentUserService currentUser)
        {
            _authService = authService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// პაციენტის თვითრეგისტრაცია ანგარიშით
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto, CancellationToken cancellationToken)
        {
            var patient = await _authService.RegisterPatientAsync(dto, cancellationToken);

            return Created($"api/patients/{patient.Id}",
                ApiResponse<object>.Succeed(patient, "Patient registered successfully."));
        }

        /// <summary>
        /// ლოგინი: აბრუნებს JWT ტოკენს და როლებს. 5 არასწორი ცდის შემდეგ ანგარიში 15 წუთით იბლოკება
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto, CancellationToken cancellationToken)
        {
            var auth = await _authService.LoginAsync(dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(auth, "Logged in successfully."));
        }

        /// <summary>
        /// ცვლის მიმდინარე მომხმარებლის პაროლს (ნებისმიერი როლი)
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedAccessException("Not authenticated");

            await _authService.ChangePasswordAsync(userId, dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Password changed successfully."));
        }

        /// <summary>
        /// აბრუნებს მიმდინარე მომხმარებლის მონაცემებს ტოკენიდან
        /// </summary>
        [HttpGet("me")]
        public IActionResult Me()
        {
            var me = new
            {
                userId = _currentUser.UserId,
                email = User.FindFirstValue(ClaimTypes.Email),
                roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value),
                doctorId = _currentUser.DoctorId,
                patientId = _currentUser.PatientId
            };

            return Ok(ApiResponse<object>.Succeed(me, "Current user retrieved successfully."));
        }
    }
}