using System.Data;
using KerniX.Api.Models.Configuracion.SerieComprobante;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class SerieComprobanteDbService
    : ISerieComprobanteDbService
{
    private readonly string _connectionString;

    public SerieComprobanteDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public Task<IReadOnlyList<SerieComprobanteResponse>>
        ListarAsync(
            int idEmpresa,
            int idSucursal)
    {
        return ListarAsync(
            "cfg.sp_SerieComprobante_Listar",
            idEmpresa,
            idSucursal);
    }

    public Task<IReadOnlyList<SerieComprobanteResponse>>
        ListarActivasAsync(
            int idEmpresa,
            int idSucursal)
    {
        return ListarAsync(
            "cfg.sp_SerieComprobante_ListarActivas",
            idEmpresa,
            idSucursal);
    }

    public async Task<SerieComprobanteResponse?>
        ObtenerPorIdAsync(
            int idSerieComprobante)
    {
        return await ObtenerAsync(
            "cfg.sp_SerieComprobante_ObtenerPorId",
            c =>
            {
                c.Parameters.Add(
                    "@IdSerieComprobante",
                    SqlDbType.Int)
                    .Value =
                    idSerieComprobante;
            });
    }

    public async Task<SerieComprobanteResponse?>
        ObtenerAsync(
            int idEmpresa,
            int idSucursal,
            string tipoComprobante,
            string serie)
    {
        return await ObtenerAsync(
            "cfg.sp_SerieComprobante_Obtener",
            c =>
            {
                AgregarBase(
                    c,
                    idEmpresa,
                    idSucursal);

                AgregarTipoSerie(
                    c,
                    tipoComprobante,
                    serie);
            });
    }

    public Task<SerieComprobanteResponse>
        CrearAsync(
            SerieComprobanteCrearRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_SerieComprobante_Crear",
            c =>
            {
                AgregarBase(
                    c,
                    request.IdEmpresa,
                    request.IdSucursal);

                AgregarTipoSerie(
                    c,
                    request.TipoComprobante,
                    request.Serie);

                c.Parameters.Add(
                    "@CorrelativoActual",
                    SqlDbType.BigInt)
                    .Value =
                    request.CorrelativoActual;

                c.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value =
                    request.Estado;
            });
    }

    public Task<SerieComprobanteResponse>
        ActualizarAsync(
            int idSerieComprobante,
            SerieComprobanteActualizarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_SerieComprobante_Actualizar",
            c =>
            {
                c.Parameters.Add(
                    "@IdSerieComprobante",
                    SqlDbType.Int)
                    .Value =
                    idSerieComprobante;

                AgregarTipoSerie(
                    c,
                    request.TipoComprobante,
                    request.Serie);

                c.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value =
                    request.Estado;
            });
    }

    public Task<SerieComprobanteResponse>
        CambiarEstadoAsync(
            int idSerieComprobante,
            bool estado)
    {
        return EjecutarAsync(
            "cfg.sp_SerieComprobante_CambiarEstado",
            c =>
            {
                c.Parameters.Add(
                    "@IdSerieComprobante",
                    SqlDbType.Int)
                    .Value =
                    idSerieComprobante;

                c.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = estado;
            });
    }

    public Task<SerieComprobanteResponse>
        ActualizarCorrelativoAsync(
            int idSerieComprobante,
            long nuevoCorrelativo)
    {
        return EjecutarAsync(
            "cfg.sp_SerieComprobante_ActualizarCorrelativo",
            c =>
            {
                c.Parameters.Add(
                    "@IdSerieComprobante",
                    SqlDbType.Int)
                    .Value =
                    idSerieComprobante;

                c.Parameters.Add(
                    "@NuevoCorrelativo",
                    SqlDbType.BigInt)
                    .Value =
                    nuevoCorrelativo;
            });
    }

    public Task<SerieComprobanteSiguienteResponse>
        ObtenerSiguienteAsync(
            int idEmpresa,
            int idSucursal,
            string tipoComprobante,
            string serie)
    {
        return EjecutarSiguienteAsync(
            "cfg.sp_SerieComprobante_ObtenerSiguiente",
            idEmpresa,
            idSucursal,
            tipoComprobante,
            serie);
    }

    public Task<SerieComprobanteSiguienteResponse>
        GenerarNumeroAsync(
            int idEmpresa,
            int idSucursal,
            string tipoComprobante,
            string serie)
    {
        return EjecutarSiguienteAsync(
            "cfg.sp_SerieComprobante_GenerarNumero",
            idEmpresa,
            idSucursal,
            tipoComprobante,
            serie);
    }

    private async Task<IReadOnlyList<SerieComprobanteResponse>>
        ListarAsync(
            string procedimiento,
            int idEmpresa,
            int idSucursal)
    {
        var lista =
            new List<SerieComprobanteResponse>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarBase(
            command,
            idEmpresa,
            idSucursal);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            lista.Add(Mapear(reader));

        return lista;
    }

    private async Task<SerieComprobanteResponse?>
        ObtenerAsync(
            string procedimiento,
            Action<SqlCommand> configurar)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        configurar(command);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? Mapear(reader)
            : null;
    }

    private async Task<SerieComprobanteResponse>
        EjecutarAsync(
            string procedimiento,
            Action<SqlCommand> configurar)
    {
        return await ObtenerAsync(
            procedimiento,
            configurar)
            ?? throw new InvalidOperationException(
                "El SP no devolvió datos.");
    }

    private async Task<SerieComprobanteSiguienteResponse>
        EjecutarSiguienteAsync(
            string procedimiento,
            int idEmpresa,
            int idSucursal,
            string tipo,
            string serie)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarBase(
            command,
            idEmpresa,
            idSucursal);

        AgregarTipoSerie(
            command,
            tipo,
            serie);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "No se generó correlativo.");
        }

        return new SerieComprobanteSiguienteResponse
        {
            IdSerieComprobante =
                Convert.ToInt32(
                    reader["id_serie_comprobante"]),

            IdEmpresa =
                TieneColumna(reader, "id_empresa")
                    ? Convert.ToInt32(
                        reader["id_empresa"])
                    : idEmpresa,

            IdSucursal =
                TieneColumna(reader, "id_sucursal")
                    ? Convert.ToInt32(
                        reader["id_sucursal"])
                    : idSucursal,

            TipoComprobante =
                reader["tipo_comprobante"]?.ToString()
                ?? tipo,

            Serie =
                reader["serie"]?.ToString()
                ?? serie,

            Correlativo =
                Convert.ToInt64(
                    reader["correlativo"]),

            NumeroComprobante =
                TieneColumna(
                    reader,
                    "numero_comprobante")
                    ? reader["numero_comprobante"]?.ToString()
                    : null
        };
    }

    private static void AgregarBase(
        SqlCommand c,
        int empresa,
        int sucursal)
    {
        c.Parameters.Add(
            "@IdEmpresa",
            SqlDbType.Int)
            .Value = empresa;

        c.Parameters.Add(
            "@IdSucursal",
            SqlDbType.Int)
            .Value = sucursal;
    }

    private static void AgregarTipoSerie(
        SqlCommand c,
        string tipo,
        string serie)
    {
        c.Parameters.Add(
            "@TipoComprobante",
            SqlDbType.NVarChar,
            40)
            .Value = tipo;

        c.Parameters.Add(
            "@Serie",
            SqlDbType.NVarChar,
            20)
            .Value = serie;
    }

    private static SerieComprobanteResponse Mapear(
        SqlDataReader r)
    {
        return new SerieComprobanteResponse
        {
            IdSerieComprobante =
                Convert.ToInt32(
                    r["id_serie_comprobante"]),

            IdEmpresa =
                Convert.ToInt32(
                    r["id_empresa"]),

            IdSucursal =
                Convert.ToInt32(
                    r["id_sucursal"]),

            TipoComprobante =
                r["tipo_comprobante"]?.ToString()
                ?? string.Empty,

            Serie =
                r["serie"]?.ToString()
                ?? string.Empty,

            CorrelativoActual =
                Convert.ToInt64(
                    r["correlativo_actual"]),

            Estado =
                Convert.ToBoolean(
                    r["estado"])
        };
    }

    private static bool TieneColumna(
        SqlDataReader r,
        string nombre)
    {
        for (var i = 0; i < r.FieldCount; i++)
        {
            if (r.GetName(i).Equals(
                nombre,
                StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}