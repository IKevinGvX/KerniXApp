using System.Data;
using KerniX.Api.Models.Configuracion.Dashboard;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class ConfiguracionDbService
    : IConfiguracionDbService
{
    private readonly string _connectionString;

    public ConfiguracionDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public async Task<ConfiguracionDiagnosticoResponse>
        ObtenerDiagnosticoAsync(
            int idEmpresa,
            int idSucursal)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_Configuracion_DiagnosticoGeneral",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarEmpresaSucursal(
            command,
            idEmpresa,
            idSucursal);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "No se obtuvo el diagnóstico.");
        }

        return new ConfiguracionDiagnosticoResponse
        {
            IdEmpresa =
                Convert.ToInt32(
                    reader["id_empresa"]),

            IdSucursal =
                Convert.ToInt32(
                    reader["id_sucursal"]),

            EmpresaExiste =
                Convert.ToBoolean(
                    reader["empresa_existe"]),

            SucursalExiste =
                Convert.ToBoolean(
                    reader["sucursal_existe"]),

            ParametrosActivos =
                Convert.ToInt32(
                    reader["parametros_activos"]),

            ConfiguracionesSucursalActivas =
                Convert.ToInt32(
                    reader["configuraciones_sucursal_activas"]),

            MonedasActivas =
                Convert.ToInt32(
                    reader["monedas_activas"]),

            ImpuestosVigentes =
                Convert.ToInt32(
                    reader["impuestos_vigentes"]),

            SeriesActivas =
                Convert.ToInt32(
                    reader["series_activas"]),

            PlanesMantenimientoActivos =
                Convert.ToInt32(
                    reader["planes_mantenimiento_activos"]),

            AlertasPendientes =
                Convert.ToInt32(
                    reader["alertas_pendientes"]),

            UltimaCopiaSeguridad =
                reader["ultima_copia_seguridad"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["ultima_copia_seguridad"]),

            UltimoMantenimientoExitoso =
                reader["ultimo_mantenimiento_exitoso"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["ultimo_mantenimiento_exitoso"])
        };
    }

    public async Task<ConfiguracionValidacionResponse>
        ValidarObligatoriosAsync(
            int idEmpresa,
            int idSucursal)
    {
        var response =
            new ConfiguracionValidacionResponse();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_Configuracion_ValidarObligatorios",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarEmpresaSucursal(
            command,
            idEmpresa,
            idSucursal);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            response.Observaciones.Add(
                new ConfiguracionObservacionResponse
                {
                    Codigo =
                        reader["codigo"]?.ToString()
                        ?? string.Empty,

                    Categoria =
                        reader["categoria"]?.ToString()
                        ?? string.Empty,

                    Descripcion =
                        reader["descripcion"]?.ToString()
                        ?? string.Empty,

                    Severidad =
                        reader["severidad"]?.ToString()
                        ?? string.Empty
                });
        }

        if (await reader.NextResultAsync()
            && await reader.ReadAsync())
        {
            response.Estado =
                new ConfiguracionEstadoResponse
                {
                    EstadoConfiguracion =
                        reader["estado_configuracion"]
                            ?.ToString()
                        ?? string.Empty,

                    TotalObservaciones =
                        Convert.ToInt32(
                            reader["total_observaciones"]),

                    Criticas =
                        Convert.ToInt32(
                            reader["criticas"]),

                    Altas =
                        Convert.ToInt32(
                            reader["altas"]),

                    Medias =
                        Convert.ToInt32(
                            reader["medias"])
                };
        }

        return response;
    }

    private static void AgregarEmpresaSucursal(
        SqlCommand command,
        int idEmpresa,
        int idSucursal)
    {
        command.Parameters.Add(
            "@IdEmpresa",
            SqlDbType.Int)
            .Value = idEmpresa;

        command.Parameters.Add(
            "@IdSucursal",
            SqlDbType.Int)
            .Value = idSucursal;
    }
}