using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Web.Responses;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>
        /// აბრუნებს ექიმის სამუშაო გრაფიკს
        /// </summary>
        [HttpGet("api/doctors/{doctorId:int}/schedules")]
        public async Task<IActionResult> GetByDoctor(int doctorId, CancellationToken cancellationToken)
        {
            var schedules = await _scheduleService.GetByDoctorAsync(doctorId, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(schedules, "Schedules retrieved successfully."));
        }

        /// <summary>
        /// ამატებს გრაფიკს ექიმს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("api/doctors/{doctorId:int}/schedules")]
        public async Task<IActionResult> Create(int doctorId, [FromBody] CreateDoctorScheduleDTO dto, CancellationToken cancellationToken)
        {
            var schedule = await _scheduleService.AddAsync(doctorId, dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = schedule.Id },
                ApiResponse<object>.Succeed(schedule, "Schedule created successfully."));
        }

        /// <summary>
        /// აბრუნებს გრაფიკს id-ის მიხედვით
        /// </summary>
        [HttpGet("api/schedules/{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var schedule = await _scheduleService.GetByIdAsync(id, cancellationToken);
            if (schedule == null)
                return NotFound(ApiResponse<object>.Fail("Schedule not found."));

            return Ok(ApiResponse<object>.Succeed(schedule, "Schedule retrieved successfully."));
        }

        /// <summary>
        /// ცვლის გრაფიკს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("api/schedules/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDoctorScheduleDTO dto, CancellationToken cancellationToken)
        {
            await _scheduleService.UpdateAsync(id, dto, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Schedule updated successfully."));
        }

        /// <summary>
        /// შლის გრაფიკს (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("api/schedules/{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _scheduleService.DeleteAsync(id, cancellationToken);

            return Ok(ApiResponse<object>.Succeed(null, "Schedule deleted successfully."));
        }
    }
}