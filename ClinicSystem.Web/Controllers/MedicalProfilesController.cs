using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:int}/medical-profile")]
    public class MedicalProfilesController : ControllerBase
    {
        private readonly IMedicalProfileService _profileService;

        public MedicalProfilesController(IMedicalProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int patientId, CancellationToken cancellationToken)
        {
            var result = await _profileService.GetByPatientIdAsync(patientId, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int patientId, [FromBody] CreateMedicalProfileDTO dto, CancellationToken cancellationToken)
        {
            var result = await _profileService.AddAsync(patientId, dto, cancellationToken);
            return CreatedAtAction(nameof(Get), new { patientId }, result);
        }

        [HttpPut]
        public async Task<IActionResult> Update(int patientId, [FromBody] UpdateMedicalProfileDTO dto, CancellationToken cancellationToken)
        {
            await _profileService.UpdateAsync(patientId, dto, cancellationToken);
            return NoContent();
        }
    }
}