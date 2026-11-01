using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
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

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _specialtyService.GetAllAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _specialtyService.GetByIdAsync(id, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpecialtyDTO dto, CancellationToken cancellationToken)
        {
            var result = await _specialtyService.AddAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSpecialtyDTO dto, CancellationToken cancellationToken)
        {
            await _specialtyService.UpdateAsync(id, dto, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _specialtyService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}