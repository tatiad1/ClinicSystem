using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    public class ConsultationsController : ControllerBase
    {
        private readonly IConsultationService _consultationService;

        public ConsultationsController(IConsultationService consultationService)
        {
            _consultationService = consultationService;
        }

        [HttpPost("api/appointments/{appointmentId:int}/consultation")]
        public async Task<IActionResult> Create(int appointmentId, [FromBody] CreateConsultationDTO dto, CancellationToken cancellationToken)
        {
            var result = await _consultationService.AddAsync(appointmentId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetByAppointment), new { appointmentId }, result);
        }

        [HttpGet("api/appointments/{appointmentId:int}/consultation")]
        public async Task<IActionResult> GetByAppointment(int appointmentId, CancellationToken cancellationToken)
        {
            var result = await _consultationService.GetByAppointmentAsync(appointmentId, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut("api/appointments/{appointmentId:int}/consultation")]
        public async Task<IActionResult> Update(int appointmentId, [FromBody] UpdateConsultationDTO dto, CancellationToken cancellationToken)
        {
            await _consultationService.UpdateAsync(appointmentId, dto, cancellationToken);
            return NoContent();
        }

        // პაციენტის სამედიცინო ისტორია
        [HttpGet("api/patients/{patientId:int}/consultations")]
        public async Task<IActionResult> GetByPatient(int patientId, CancellationToken cancellationToken)
        {
            var result = await _consultationService.GetByPatientAsync(patientId, cancellationToken);
            return Ok(result);
        }
    }
}