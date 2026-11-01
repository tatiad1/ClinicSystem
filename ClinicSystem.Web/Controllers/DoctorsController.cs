using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorsController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        // GET api/doctors  ან  GET api/doctors?specialtyId=1
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? specialtyId, CancellationToken cancellationToken)
        {
            var result = await _doctorService.GetAllAsync(specialtyId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _doctorService.GetByIdAsync(id, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDoctorDTO dto, CancellationToken cancellationToken)
        {
            var result = await _doctorService.AddAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDoctorDTO dto, CancellationToken cancellationToken)
        {
            await _doctorService.UpdateAsync(id, dto, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
        {
            await _doctorService.SetActiveAsync(id, false, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
        {
            await _doctorService.SetActiveAsync(id, true, cancellationToken);
            return NoContent();
        }
    }
}