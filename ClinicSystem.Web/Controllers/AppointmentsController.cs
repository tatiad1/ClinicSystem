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
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ICurrentUserService _currentUser;

        public AppointmentsController(IAppointmentService appointmentService, ICurrentUserService currentUser)
        {
            _appointmentService = appointmentService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// ჩაწერს პაციენტს ვიზიტზე. პაციენტი მხოლოდ საკუთარ თავს წერს,
        /// მოთხოვნაში მითითებული patientId იგნორირდება
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist},{Roles.Patient}")]
        [HttpPost("api/appointments")]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDTO dto, CancellationToken cancellationToken)
        {
            if (_currentUser.IsInRole(Roles.Patient))
            {
                dto.PatientId = _currentUser.PatientId
                    ?? throw new ForbiddenException("Patient profile not found for this account");
            }

            var appointment = await _appointmentService.AddAsync(dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = appointment.Id },
                ApiResponse<object>.Succeed(appointment, "Appointment booked successfully."));
        }

        /// <summary>
        /// აბრუნებს ვიზიტს id-ის მიხედვით (ვიზიტის პაციენტს, ექიმს ან პერსონალს)
        /// </summary>
        [HttpGet("api/appointments/{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);
            if (appointment == null)
                return NotFound(ApiResponse<object>.Fail("Appointment not found."));

            _currentUser.EnsureCanAccessAppointment(appointment.PatientId, appointment.DoctorId);

            return Ok(ApiResponse<object>.Succeed(appointment, "Appointment retrieved successfully."));
        }

        /// <summary>
        /// აბრუნებს პაციენტის ყველა ვიზიტს
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist},{Roles.Doctor},{Roles.Patient}")]
        [HttpGet("api/patients/{patientId:int}/appointments")]
        public async Task<IActionResult> GetByPatient(int patientId, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(patientId, cancellationToken);

            var appointments = await _appointmentService.GetByPatientAsync(patientId, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(appointments, "Appointments retrieved successfully."));
        }

        /// <summary>
        /// აბრუნებს ექიმის ვიზიტებს, არასავალდებულო ფილტრით თარიღის მიხედვით
        /// </summary>
        /// <param name="date">თარიღი yyyy-MM-dd ფორმატით (არასავალდებულო)</param>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist},{Roles.Doctor}")]
        [HttpGet("api/doctors/{doctorId:int}/appointments")]
        public async Task<IActionResult> GetByDoctor(int doctorId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
        {
            _currentUser.EnsureCanAccessDoctor(doctorId);

            var appointments = await _appointmentService.GetByDoctorAsync(doctorId, date, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(appointments, "Appointments retrieved successfully."));
        }

        /// <summary>
        /// აბრუნებს ექიმის თავისუფალ დროებს ერთ დღეში (ნახევარსაათიანი ინტერვალებით)
        /// </summary>
        /// <param name="date">თარიღი yyyy-MM-dd ფორმატით</param>
        [HttpGet("api/doctors/{doctorId:int}/available-slots")]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
        {
            if (date == null)
                return BadRequest(ApiResponse<object>.Fail("Query parameter 'date' is required (format: yyyy-MM-dd)"));

            var slots = await _appointmentService.GetAvailableSlotsAsync(doctorId, date.Value, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(slots, "Available slots retrieved successfully."));
        }

        /// <summary>
        /// აუქმებს ვიზიტს, სანამ ის დაწყებული არ არის. მიმართვა თავისუფლდება
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist},{Roles.Doctor},{Roles.Patient}")]
        [HttpPost("api/appointments/{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            await EnsureAppointmentAccessAsync(id, cancellationToken);

            await _appointmentService.CancelAsync(id, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Appointment cancelled successfully."));
        }

        /// <summary>
        /// აღნიშნავს, რომ პაციენტი ვიზიტზე არ მოვიდა (მხოლოდ ვიზიტის დროის შემდეგ)
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist},{Roles.Doctor}")]
        [HttpPost("api/appointments/{id:int}/no-show")]
        public async Task<IActionResult> NoShow(int id, CancellationToken cancellationToken)
        {
            await EnsureAppointmentAccessAsync(id, cancellationToken);

            await _appointmentService.MarkNoShowAsync(id, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Appointment marked as no-show successfully."));
        }

        // ვიზიტს ჯერ ვტვირთავთ და ვამოწმებთ, ეკუთვნის თუ არა ამ მომხმარებელს
        private async Task EnsureAppointmentAccessAsync(int appointmentId, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentService.GetByIdAsync(appointmentId, cancellationToken)
                ?? throw new KeyNotFoundException("Appointment not found");

            _currentUser.EnsureCanAccessAppointment(appointment.PatientId, appointment.DoctorId);
        }
    }
}
