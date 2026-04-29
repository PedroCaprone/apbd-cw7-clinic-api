using Microsoft.AspNetCore.Mvc;
using apbd_cw7_clinic_api.Services;

namespace apbd_cw7_clinic_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly DbService _dbService;

    public AppointmentsController(DbService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] string? status, [FromQuery] string? patientLastName)
    {
        var appointments = await _dbService.GetAppointmentsAsync(status, patientLastName);
        return Ok(appointments);
    }
    
    [HttpGet("{idAppointment:int}")]
    public async Task<IActionResult> GetAppointmentById(int idAppointment)
    {
        var appointment = await _dbService.GetAppointmentByIdAsync(idAppointment);

        if (appointment is null)
        {
            return NotFound();
        }

        return Ok(appointment);
    }
}