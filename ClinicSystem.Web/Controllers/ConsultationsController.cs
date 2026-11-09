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
    public class ConsultationsController : ControllerBase
    {
        private readonly IConsultationService _consultationService;
        private readonly IAppointmentService _appointmentService;
        private readonly ICurrentUserService _currentUser;

        public ConsultationsController(
            IConsultationService consultationService,
            IAppointmentService appointmentService,
            ICurrentUserService currentUser)
        {
            _consultationService = consultationService;
            _appointmentService = appointmentService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// წერს კონსულტაციას დადასტურებულ ვიზიტზე, ვიზიტი ხდება Completed.
        /// წერს მხოლოდ იმ ვიზიტის ექიმი
        /// </summary>
        [Authorize(Roles = Roles.Doctor)]
        [HttpPost("api/appointments/{appointmentId:int}/consultation")]
        public async Task<IActionResult> Create(int appointmentId, [FromBody] CreateConsultationDTO dto, CancellationToken cancellationToken)
        {
            await EnsureAppointmentDoctorAsync(appointmentId, cancellationToken);

            var consultation = await _consultationService.AddAsync(appointmentId, dto, cancellationToken);

            return CreatedAtAction(nameof(GetByAppointment), new { appointmentId },
                ApiResponse<object>.Succeed(consultation, "Consultation created successfully."));
        }

        /// <summary>
        /// აბრუნებს ვიზიტის კონსულტაციას
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Doctor},{Roles.Patient}")]
        [HttpGet("api/appointments/{appointmentId:int}/consultation")]
        public async Task<IActionResult> GetByAppointment(int appointmentId, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentService.GetByIdAsync(appointmentId, cancellationToken)
                ?? throw new KeyNotFoundException("Appointment not found");

            _currentUser.EnsureCanAccessAppointment(appointment.PatientId, appointment.DoctorId);

            var consultation = await _consultationService.GetByAppointmentAsync(appointmentId, cancellationToken);
            if (consultation == null)
                return NotFound(ApiResponse<object>.Fail("Consultation not found."));

            return Ok(ApiResponse<object>.Succeed(consultation, "Consultation retrieved successfully."));
        }

        /// <summary>
        /// ასწორებს დიაგნოზს და დანიშნულებას (მხოლოდ იმ ვიზიტის ექიმი)
        /// </summary>
        [Authorize(Roles = Roles.Doctor)]
        [HttpPut("api/appointments/{appointmentId:int}/consultation")]
        public async Task<IActionResult> Update(int appointmentId, [FromBody] UpdateConsultationDTO dto, CancellationToken cancellationToken)
        {
            await EnsureAppointmentDoctorAsync(appointmentId, cancellationToken);

            await _consultationService.UpdateAsync(appointmentId, dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Consultation updated successfully."));
        }

        /// <summary>
        /// აბრუნებს პაციენტის სამედიცინო ისტორიას (ყველა კონსულტაცია, უახლესი პირველი)
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Doctor},{Roles.Patient}")]
        [HttpGet("api/patients/{patientId:int}/consultations")]
        public async Task<IActionResult> GetByPatient(int patientId, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(patientId, cancellationToken);

            var consultations = await _consultationService.GetByPatientAsync(patientId, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(consultations, "Consultations retrieved successfully."));
        }

        private async Task EnsureAppointmentDoctorAsync(int appointmentId, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentService.GetByIdAsync(appointmentId, cancellationToken)
                ?? throw new KeyNotFoundException("Appointment not found");

            if (appointment.DoctorId != _currentUser.DoctorId)
                throw new ForbiddenException("Only the doctor of this appointment can write its consultation");
        }
    }
}
