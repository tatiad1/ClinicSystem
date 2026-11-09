using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Web.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ICurrentUserService _currentUser;

        public PatientsController(IPatientService patientService, ICurrentUserService currentUser)
        {
            _patientService = patientService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// აბრუნებს ყველა პაციენტს, არასავალდებულო ფილტრით ოჯახის ექიმის მიხედვით
        /// </summary>
        /// <param name="familyDoctorId">ოჯახის ექიმის id (არასავალდებულო)</param>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist},{Roles.Doctor}")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? familyDoctorId, CancellationToken cancellationToken)
        {
            var patients = _currentUser.IsInRole(Roles.Doctor)
                ? await _patientService.GetAccessibleToDoctorAsync(
                    _currentUser.DoctorId
                        ?? throw new ClinicSystem.Application.Exceptions.ForbiddenException(
                            "Doctor profile not found for this account"),
                    cancellationToken)
                : await _patientService.GetAllAsync(familyDoctorId, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(patients, "Patients retrieved successfully."));
        }

        /// <summary>
        /// აბრუნებს პაციენტს id-ის მიხედვით. პაციენტი მხოლოდ საკუთარ თავს ხედავს
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(id, cancellationToken);

            var patient = await _patientService.GetByIdAsync(id, cancellationToken);
            if (patient == null)
                return NotFound(ApiResponse<object>.Fail("Patient not found."));

            return Ok(ApiResponse<object>.Succeed(patient, "Patient retrieved successfully."));
        }

        /// <summary>
        /// ქმნის პაციენტს, ანგარიში არასავალდებულოა (ადმინი, რეგისტრატორი)
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePatientDTO dto, CancellationToken cancellationToken)
        {
            var patient = await _patientService.AddAsync(dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = patient.Id },
                ApiResponse<object>.Succeed(patient, "Patient created successfully."));
        }

        /// <summary>
        /// ქმნის ანგარიშს პაციენტზე, რომელსაც ის ჯერ არ აქვს, და ანიჭებს Patient როლს (ადმინი, რეგისტრატორი)
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist}")]
        [HttpPost("{id:int}/account")]
        public async Task<IActionResult> CreateAccount(int id, [FromBody] CreatePatientAccountDTO dto, CancellationToken cancellationToken)
        {
            var patient = await _patientService.CreateAccountAsync(id, dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id },
                ApiResponse<object>.Succeed(patient, "Patient account created successfully."));
        }

        /// <summary>
        /// ცვლის პაციენტის მონაცემებს. პაციენტს შეუძლია მხოლოდ საკუთარი
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist},{Roles.Patient}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientDTO dto, CancellationToken cancellationToken)
        {
            await _currentUser.EnsureCanAccessPatientAsync(id, cancellationToken);

            await _patientService.UpdateAsync(id, dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Patient updated successfully."));
        }

        /// <summary>
        /// ანიჭებს ან ხსნის პაციენტის ოჯახის ექიმს (doctorId = null ხსნის). მხოლოდ ადმინი და რეგისტრატორი
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Receptionist}")]
        [HttpPut("{id:int}/family-doctor")]
        public async Task<IActionResult> AssignFamilyDoctor(int id, [FromBody] AssignFamilyDoctorDTO dto, CancellationToken cancellationToken)
        {
            await _patientService.AssignFamilyDoctorAsync(id, dto.DoctorId, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Family doctor updated successfully."));
        }
    }
}
