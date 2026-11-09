using ClinicSystem.Application.Exceptions;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Web.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    public class ReferralsController : ControllerBase
    {
        private readonly IReferralService _referralService;
        private readonly ICurrentUserService _currentUser;

        public ReferralsController(IReferralService referralService, ICurrentUserService currentUser)
        {
            _referralService = referralService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// აბრუნებს პაციენტის მიმართვებს
        /// </summary>
        /// <param name="activeOnly">true: მხოლოდ გამოუყენებელი და ვადაგაუსვლელი</param>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Doctor},{Roles.Patient}")]
        [HttpGet("api/patients/{patientId:int}/referrals")]
        public async Task<IActionResult> GetByPatient(int patientId, [FromQuery] bool activeOnly, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(patientId, cancellationToken);

            var referrals = await _referralService.GetByPatientAsync(patientId, activeOnly, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(referrals, "Referrals retrieved successfully."));
        }

        /// <summary>
        /// აძლევს პაციენტს მიმართვას სხვა სპეციალისტთან. გასცემს მხოლოდ შესული ექიმი,
        /// რომელიც პაციენტის ოჯახის ექიმია
        /// </summary>
        [Authorize(Roles = Roles.Doctor)]
        [HttpPost("api/patients/{patientId:int}/referrals")]
        public async Task<IActionResult> Create(int patientId, [FromBody] CreateReferralDTO dto, CancellationToken cancellationToken)
        {
            // მიმართვას გასცემს ის ექიმი, ვინც შესულია სისტემაში
            dto.FromDoctorId = _currentUser.DoctorId
                ?? throw new ForbiddenException("Doctor profile not found for this account");

            var referral = await _referralService.AddAsync(patientId, dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = referral.Id },
                ApiResponse<object>.Succeed(referral, "Referral created successfully."));
        }

        /// <summary>
        /// აბრუნებს მიმართვას id-ის მიხედვით
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Doctor},{Roles.Patient}")]
        [HttpGet("api/referrals/{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var referral = await _referralService.GetByIdAsync(id, cancellationToken);
            if (referral == null)
                return NotFound(ApiResponse<object>.Fail("Referral not found."));

            await _currentUser.EnsureCanAccessPatientAsync(referral.PatientId, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(referral, "Referral retrieved successfully."));
        }
    }
}
