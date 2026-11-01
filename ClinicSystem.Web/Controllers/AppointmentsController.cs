using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost("api/appointments")]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDTO dto, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.AddAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("api/appointments/{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetByIdAsync(id, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("api/patients/{patientId:int}/appointments")]
        public async Task<IActionResult> GetByPatient(int patientId, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetByPatientAsync(patientId, cancellationToken);
            return Ok(result);
        }

        // GET api/doctors/1/appointments  ან  GET api/doctors/1/appointments?date=2026-11-09
        [HttpGet("api/doctors/{doctorId:int}/appointments")]
        public async Task<IActionResult> GetByDoctor(int doctorId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetByDoctorAsync(doctorId, date, cancellationToken);
            return Ok(result);
        }

        // GET api/doctors/1/available-slots?date=2026-11-09
        [HttpGet("api/doctors/{doctorId:int}/available-slots")]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
        {
            if (date == null)
                return BadRequest(new { error = "Query parameter 'date' is required (format: yyyy-MM-dd)" });

            var result = await _appointmentService.GetAvailableSlotsAsync(doctorId, date.Value, cancellationToken);
            return Ok(result);
        }

        [HttpPost("api/appointments/{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            await _appointmentService.ApproveAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("api/appointments/{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
        {
            await _appointmentService.RejectAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("api/appointments/{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            await _appointmentService.CancelAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("api/appointments/{id:int}/no-show")]
        public async Task<IActionResult> NoShow(int id, CancellationToken cancellationToken)
        {
            await _appointmentService.MarkNoShowAsync(id, cancellationToken);
            return NoContent();
        }
    }
}