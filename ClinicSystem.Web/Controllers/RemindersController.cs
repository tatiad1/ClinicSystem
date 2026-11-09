using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Models.Identity;
using ClinicSystem.Web.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemindersController : ControllerBase
    {
        private readonly IAppointmentReminderService _reminderService;

        public RemindersController(IAppointmentReminderService reminderService)
        {
            _reminderService = reminderService;
        }

        /// <summary>
        /// ხელით უშვებს შეხსენებების გაგზავნას (მხოლოდ ადმინი)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("run")]
        public async Task<IActionResult> Run(CancellationToken cancellationToken)
        {
            var sent = await _reminderService.SendDueRemindersAsync(cancellationToken);

            return Ok(ApiResponse<object>.Succeed(new { sent }, "Reminders processed successfully."));
        }
    }
}