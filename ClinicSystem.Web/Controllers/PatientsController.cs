using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // GET api/patients  ან  GET api/patients?familyDoctorId=1
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? familyDoctorId, CancellationToken cancellationToken)
        {
            var result = await _patientService.GetAllAsync(familyDoctorId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _patientService.GetByIdAsync(id, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePatientDTO dto, CancellationToken cancellationToken)
        {
            var result = await _patientService.AddAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientDTO dto, CancellationToken cancellationToken)
        {
            await _patientService.UpdateAsync(id, dto, cancellationToken);
            return NoContent();
        }

        [HttpPut("{id:int}/family-doctor")]
        public async Task<IActionResult> AssignFamilyDoctor(int id, [FromBody] AssignFamilyDoctorDTO dto, CancellationToken cancellationToken)
        {
            await _patientService.AssignFamilyDoctorAsync(id, dto.DoctorId, cancellationToken);
            return NoContent();
        }
    }
}