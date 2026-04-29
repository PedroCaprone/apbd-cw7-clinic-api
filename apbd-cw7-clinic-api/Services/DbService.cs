using Microsoft.Data.SqlClient;

namespace apbd_cw7_clinic_api.Services;

public class DbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task TestConnectionAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        Console.WriteLine("Connected to database!!");
    }
}