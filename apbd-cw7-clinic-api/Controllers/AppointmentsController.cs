using apbd_cw7_clinic_api.DTOs;
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
    
    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Reason cannot be empty"
            });
        }

        if (dto.Reason.Length > 250)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Reason cannot exceed 250 characters"
            });
        }

        if (dto.AppointmentDate < DateTime.Now)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Appointment date cannot be in the past"
            });
        }
        
        if (!await _dbService.ActivePatientExistsAsync(dto.IdPatient))
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Patient does not exist or is inactive"
            });
        }

        if (!await _dbService.ActiveDoctorExistsAsync(dto.IdDoctor))
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Doctor does not exist or is inactive"
            });
        }
        
        if (await _dbService.DoctorHasScheduledAppointmentAtAsync(dto.IdDoctor, dto.AppointmentDate))
        {
            return Conflict(new ErrorResponseDto
            {
                Message = "Doctor already has a scheduled appointment at this time"
            });
        }

        var newAppointmentId = await _dbService.CreateAppointmentAsync(dto);

        return CreatedAtAction(
            nameof(GetAppointmentById),
            new { idAppointment = newAppointmentId },
            new { idAppointment = newAppointmentId }
        );
    }
    
    [HttpPut("{idAppointment:int}")]
    public async Task<IActionResult> UpdateAppointment(int idAppointment, [FromBody] UpdateAppointmentRequestDto dto)
    {
        var currentStatus = await _dbService.GetAppointmentStatusAsync(idAppointment);
        
        if (currentStatus == "Completed" && dto.AppointmentDate != (await _dbService.GetAppointmentByIdAsync(idAppointment))!.AppointmentDate)
        {
            return Conflict(new ErrorResponseDto
            {
                Message = "Cannot change date of a completed appointment"
            });
        }

        if (currentStatus is null)
        {
            return NotFound();
        }
        
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Reason cannot be empty"
            });
        }

        if (dto.Reason.Length > 250)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Reason cannot exceed 250 characters"
            });
        }

        if (dto.Status is not "Scheduled" and not "Completed" and not "Cancelled")
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Invalid appointment status"
            });
        }
        
        if (!await _dbService.ActivePatientExistsAsync(dto.IdPatient))
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Patient does not exist or is inactive"
            });
        }

        if (!await _dbService.ActiveDoctorExistsAsync(dto.IdDoctor))
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = "Doctor does not exist or is inactive"
            });
        }
        
        if (await _dbService.DoctorHasConflictAtAsync(dto.IdDoctor, dto.AppointmentDate, idAppointment))
        {
            return Conflict(new ErrorResponseDto
            {
                Message = "Doctor already has a scheduled appointment at this time"
            });
        }

        await _dbService.UpdateAppointmentAsync(idAppointment, dto);

        return Ok(new
        {
            message = "Appointment updated successfully"
        });
    }
}