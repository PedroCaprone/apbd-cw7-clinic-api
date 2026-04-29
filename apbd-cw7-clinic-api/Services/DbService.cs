using apbd_cw7_clinic_api.DTOs;
using Microsoft.Data.SqlClient;

namespace apbd_cw7_clinic_api.Services;

public class DbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<List<AppointmentListDto>> GetAppointmentsAsync(string? status, string? patientLastName)
    {
        var appointments = new List<AppointmentListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("""
                                                 SELECT
                                                     a.IdAppointment,
                                                     a.AppointmentDate,
                                                     a.Status,
                                                     a.Reason,
                                                     p.FirstName + N' ' + p.LastName AS PatientFullName,
                                                     p.Email AS PatientEmail
                                                 FROM dbo.Appointments a
                                                 JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                                                 WHERE (@Status IS NULL OR a.Status = @Status)
                                                   AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                                                 ORDER BY a.AppointmentDate;
                                                 """, connection);
        command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            appointments.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            });
        }

        return appointments;
    }
    public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("""
                                                 SELECT
                                                     a.IdAppointment,
                                                     a.AppointmentDate,
                                                     a.Status,
                                                     a.Reason,
                                                     a.InternalNotes,
                                                     a.CreatedAt,
                                                     p.IdPatient,
                                                     p.FirstName + N' ' + p.LastName AS PatientFullName,
                                                     p.Email AS PatientEmail,
                                                     p.PhoneNumber AS PatientPhoneNumber,
                                                     d.IdDoctor,
                                                     d.FirstName + N' ' + d.LastName AS DoctorFullName,
                                                     d.LicenseNumber AS DoctorLicenseNumber,
                                                     s.Name AS DoctorSpecialization
                                                 FROM dbo.Appointments a
                                                 JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                                                 JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
                                                 JOIN dbo.Specializations s ON s.IdSpecialization = d.IdSpecialization
                                                 WHERE a.IdAppointment = @IdAppointment;
                                                 """, connection);

        command.Parameters.AddWithValue("@IdAppointment", idAppointment);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AppointmentDetailsDto
        {
            IdAppointment = reader.GetInt32(0),
            AppointmentDate = reader.GetDateTime(1),
            Status = reader.GetString(2),
            Reason = reader.GetString(3),
            InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5),

            IdPatient = reader.GetInt32(6),
            PatientFullName = reader.GetString(7),
            PatientEmail = reader.GetString(8),
            PatientPhoneNumber = reader.GetString(9),

            IdDoctor = reader.GetInt32(10),
            DoctorFullName = reader.GetString(11),
            DoctorLicenseNumber = reader.GetString(12),
            DoctorSpecialization = reader.GetString(13)
        };
    }
    
    public async Task<bool> ActivePatientExistsAsync(int idPatient)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("""
                                                 SELECT COUNT(1)
                                                 FROM dbo.Patients
                                                 WHERE IdPatient = @IdPatient AND IsActive = 1;
                                                 """, connection);

        command.Parameters.AddWithValue("@IdPatient", idPatient);

        var result = (int)await command.ExecuteScalarAsync();
        return result > 0;
    }

    public async Task<bool> ActiveDoctorExistsAsync(int idDoctor)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("""
                                                 SELECT COUNT(1)
                                                 FROM dbo.Doctors
                                                 WHERE IdDoctor = @IdDoctor AND IsActive = 1;
                                                 """, connection);

        command.Parameters.AddWithValue("@IdDoctor", idDoctor);

        var result = (int)await command.ExecuteScalarAsync();
        return result > 0;
    }
    
    public async Task<bool> DoctorHasScheduledAppointmentAtAsync(int idDoctor, DateTime appointmentDate)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("""
                                                 SELECT COUNT(1)
                                                 FROM dbo.Appointments
                                                 WHERE IdDoctor = @IdDoctor
                                                   AND AppointmentDate = @AppointmentDate
                                                   AND Status = N'Scheduled';
                                                 """, connection);

        command.Parameters.AddWithValue("@IdDoctor", idDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", appointmentDate);

        var result = (int)await command.ExecuteScalarAsync();
        return result > 0;
    }
    
    public async Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("""
            INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
            OUTPUT INSERTED.IdAppointment
            VALUES (@IdPatient, @IdDoctor, @AppointmentDate, N'Scheduled', @Reason);
            """, connection);

        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        command.Parameters.AddWithValue("@Reason", dto.Reason);

        var newId = (int)await command.ExecuteScalarAsync();
        return newId;
    }
}