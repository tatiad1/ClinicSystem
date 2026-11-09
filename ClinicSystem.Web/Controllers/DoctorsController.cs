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
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly ICurrentUserService _currentUser;

        public DoctorsController(IDoctorService doctorService, ICurrentUserService currentUser)
        {
            _doctorService = doctorService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// აბრუნებს ექიმებს. ნაგულისხმევად მხოლოდ აქტიურებს; includeInactive=true მოქმედებს მხოლოდ ადმინისა და რეგისტრატორისთვის
        /// </summary>
        /// <param name="specialtyId">სპეციალობის id (არასავალდებულო)</param>
        /// <param name="includeInactive">გათიშული ექიმების ჩვენება (ადმინი, რეგისტრატორი)</param>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? specialtyId, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            var canSeeInactive = includeInactive
                && (_currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Receptionist));

            var doctors = await _doctorService.GetAllAsync(specialtyId, canSeeInactive, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(doctors, "Doctors retrieved successfully."));
        }

        /// <summary>
        /// აბრუნებს ექიმს id-ის მიხედვით (გათიშულსაც, ისტორიისთვის)
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
            if (doctor == null)
                return NotFound(ApiResponse<object>.Fail("Doctor not found."));

            return Ok(ApiResponse<object>.Succeed(doctor, "Doctor retrieved successfully."));
        }

        /// <summary>
        /// ქმნის ახალ ექიმს ანგარიშით და "Doctor" როლით (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDoctorDTO dto, CancellationToken cancellationToken)
        {
            var doctor = await _doctorService.AddAsync(dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = doctor.Id },
                ApiResponse<object>.Succeed(doctor, "Doctor created successfully."));
        }

        /// <summary>
        /// ცვლის ექიმის სახელს, გვარს, ტელეფონს და სპეციალობას (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDoctorDTO dto, CancellationToken cancellationToken)
        {
            await _doctorService.UpdateAsync(id, dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Doctor updated successfully."));
        }

        /// <summary>
        /// თიშავს ექიმს და მის ანგარიშს. შეუძლებელია, სანამ მომავალი აქტიური ვიზიტები აქვს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
        {
            await _doctorService.SetActiveAsync(id, false, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Doctor deactivated successfully."));
        }

        /// <summary>
        /// ხელახლა ააქტიურებს ექიმს და მის ანგარიშს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
        {
            await _doctorService.SetActiveAsync(id, true, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Doctor activated successfully."));
        }
    }
}