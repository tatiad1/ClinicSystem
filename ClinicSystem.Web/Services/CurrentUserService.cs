using System.Security.Claims;
using ClinicSystem.Application.Exceptions;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Identity;

namespace ClinicSystem.Web.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPatientRepository _patientRepository;
        private readonly IAppointmentRepository _appointmentRepository;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            IPatientRepository patientRepository,
            IAppointmentRepository appointmentRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _patientRepository = patientRepository;
            _appointmentRepository = appointmentRepository;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public int? DoctorId => ReadInt(ClinicClaims.DoctorId);

        public int? PatientId => ReadInt(ClinicClaims.PatientId);

        public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

        public async Task EnsureCanAccessPatientAsync(
            int patientId,
            CancellationToken cancellationToken = default)
        {
            if (IsInRole(Roles.Admin) || IsInRole(Roles.Receptionist))
                return;

            if (IsInRole(Roles.Patient) && PatientId == patientId)
                return;

            if (IsInRole(Roles.Doctor) && DoctorId is int doctorId)
            {
                var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
                if (patient == null)
                    throw new KeyNotFoundException("Patient not found");

                if (patient.FamilyDoctorId == doctorId)
                    return;

                var hasRelevantAppointment = await _appointmentRepository.ExistsAsync(
                    a => a.PatientId == patientId
                        && a.DoctorId == doctorId
                        && (a.Status == AppointmentStatus.Approved
                            || a.Status == AppointmentStatus.Completed),
                    cancellationToken);

                if (hasRelevantAppointment)
                    return;
            }

            throw new ForbiddenException("You do not have access to this patient's data");
        }

        public void EnsureCanAccessDoctor(int doctorId)
        {
            if (IsInRole(Roles.Admin) || IsInRole(Roles.Receptionist))
                return;

            if (IsInRole(Roles.Doctor) && DoctorId == doctorId)
                return;

            throw new ForbiddenException("You do not have access to this doctor's data");
        }

        public void EnsureCanAccessAppointment(int patientId, int doctorId)
        {
            if (IsInRole(Roles.Admin) || IsInRole(Roles.Receptionist))
                return;

            if (IsInRole(Roles.Doctor) && DoctorId == doctorId)
                return;

            if (IsInRole(Roles.Patient) && PatientId == patientId)
                return;

            throw new ForbiddenException("You do not have access to this appointment");
        }

        private int? ReadInt(string claimType)
        {
            var value = User?.FindFirstValue(claimType);
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
