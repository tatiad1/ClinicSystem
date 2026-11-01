using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    public class ReferralsController : ControllerBase
    {
        private readonly IReferralService _referralService;

        public ReferralsController(IReferralService referralService)
        {
            _referralService = referralService;
        }

        // GET api/patients/1/referrals  ან  GET api/patients/1/referrals?activeOnly=true
        [HttpGet("api/patients/{patientId:int}/referrals")]
        public async Task<IActionResult> GetByPatient(int patientId, [FromQuery] bool activeOnly, CancellationToken cancellationToken)
        {
            var result = await _referralService.GetByPatientAsync(patientId, activeOnly, cancellationToken);
            return Ok(result);
        }

        [HttpPost("api/patients/{patientId:int}/referrals")]
        public async Task<IActionResult> Create(int patientId, [FromBody] CreateReferralDTO dto, CancellationToken cancellationToken)
        {
            var result = await _referralService.AddAsync(patientId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("api/referrals/{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _referralService.GetByIdAsync(id, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}