using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Web.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:int}/medical-profile")]
    public class MedicalProfilesController : ControllerBase
    {
        private readonly IMedicalProfileService _profileService;
        private readonly ICurrentUserService _currentUser;

        public MedicalProfilesController(IMedicalProfileService profileService, ICurrentUserService currentUser)
        {
            _profileService = profileService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// აბრუნებს პაციენტის სამედიცინო ბარათს
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Doctor},{Roles.Patient}")]
        [HttpGet]
        public async Task<IActionResult> Get(int patientId, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(patientId, cancellationToken);

            var profile = await _profileService.GetByPatientIdAsync(patientId, cancellationToken);
            if (profile == null)
                return NotFound(ApiResponse<object>.Fail("Medical profile not found."));

            return Ok(ApiResponse<object>.Succeed(profile, "Medical profile retrieved successfully."));
        }

        /// <summary>
        /// ქმნის სამედიცინო ბარათს, ერთ პაციენტს მხოლოდ ერთი ექნება (ადმინი, ექიმი)
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Doctor}")]
        [HttpPost]
        public async Task<IActionResult> Create(int patientId, [FromBody] CreateMedicalProfileDTO dto, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(patientId, cancellationToken);

            var profile = await _profileService.AddAsync(patientId, dto, cancellationToken);

            return CreatedAtAction(nameof(Get), new { patientId },
                ApiResponse<object>.Succeed(profile, "Medical profile created successfully."));
        }

        /// <summary>
        /// ცვლის სამედიცინო ბარათს (ადმინი, ექიმი)
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Doctor}")]
        [HttpPut]
        public async Task<IActionResult> Update(int patientId, [FromBody] UpdateMedicalProfileDTO dto, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(patientId, cancellationToken);

            await _profileService.UpdateAsync(patientId, dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Medical profile updated successfully."));
        }
    }
}
