using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    public class DoctorSchedulesController : ControllerBase
    {
        private readonly IDoctorScheduleService _scheduleService;

        public DoctorSchedulesController(IDoctorScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet("api/doctors/{doctorId:int}/schedules")]
        public async Task<IActionResult> GetByDoctor(int doctorId, CancellationToken cancellationToken)
        {
            var result = await _scheduleService.GetByDoctorAsync(doctorId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("api/doctors/{doctorId:int}/schedules")]
        public async Task<IActionResult> Create(int doctorId, [FromBody] CreateDoctorScheduleDTO dto, CancellationToken cancellationToken)
        {
            var result = await _scheduleService.AddAsync(doctorId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("api/schedules/{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _scheduleService.GetByIdAsync(id, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut("api/schedules/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDoctorScheduleDTO dto, CancellationToken cancellationToken)
        {
            await _scheduleService.UpdateAsync(id, dto, cancellationToken);
            return NoContent();
        }

        [HttpDelete("api/schedules/{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _scheduleService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}