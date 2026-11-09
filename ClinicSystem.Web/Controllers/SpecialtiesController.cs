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
    public class SpecialtiesController : ControllerBase
    {
        private readonly ISpecialtyService _specialtyService;

        public SpecialtiesController(ISpecialtyService specialtyService)
        {
            _specialtyService = specialtyService;
        }

        /// <summary>
        /// აბრუნებს ყველა სპეციალობას
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var specialties = await _specialtyService.GetAllAsync(cancellationToken);

            return Ok(ApiResponse<object>.Succeed(specialties, "Specialties retrieved successfully."));
        }

        /// <summary>
        /// აბრუნებს სპეციალობას id-ის მიხედვით
        /// </summary>
        /// <param name="id">სპეციალობის უნიკალური იდენტიფიკატორი</param>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var specialty = await _specialtyService.GetByIdAsync(id, cancellationToken);
            if (specialty == null)
                return NotFound(ApiResponse<object>.Fail("Specialty not found."));

            return Ok(ApiResponse<object>.Succeed(specialty, "Specialty retrieved successfully."));
        }

        /// <summary>
        /// ქმნის ახალ სპეციალობას (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpecialtyDTO dto, CancellationToken cancellationToken)
        {
            var specialty = await _specialtyService.AddAsync(dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = specialty.Id },
                ApiResponse<object>.Succeed(specialty, "Specialty created successfully."));
        }

        /// <summary>
        /// ცვლის სპეციალობის სახელს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSpecialtyDTO dto, CancellationToken cancellationToken)
        {
            await _specialtyService.UpdateAsync(id, dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Specialty updated successfully."));
        }

        /// <summary>
        /// შლის სპეციალობას, თუ მას ექიმები არ ჰყავს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _specialtyService.DeleteAsync(id, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Specialty deleted successfully."));
        }
    }
}