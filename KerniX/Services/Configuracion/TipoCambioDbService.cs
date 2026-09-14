using System.Data;
using KerniX.Api.Models.Configuracion.TipoCambio;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class TipoCambioDbService
    : ITipoCambioDbService
{
    private readonly string _connectionString;

    public TipoCambioDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public async Task<IReadOnlyList<TipoCambioResponse>>
        ListarAsync()
    {
        return await ListarAsync(
            "cfg.sp_TipoCambio_Listar",
            null);
    }

    public async Task<TipoCambioResponse?>
        ObtenerPorIdAsync(long idTipoCambio)
    {
        return await ObtenerAsync(
            "cfg.sp_TipoCambio_ObtenerPorId",
            c =>
            {
                c.Parameters.Add(
                    "@IdTipoCambio",
                    SqlDbType.BigInt)
                    .Value = idTipoCambio;
            });
    }

    public async Task<TipoCambioResponse?>
        ObtenerPorFechaAsync(
            int idMonedaOrigen,
            int idMonedaDestino,
            DateTime fecha)
    {
        return await ObtenerAsync(
            "cfg.sp_TipoCambio_ObtenerPorFecha",
            c =>
            {
                AgregarMonedas(
                    c,
                    idMonedaOrigen,
                    idMonedaDestino);

                c.Parameters.Add(
                    "@Fecha",
                    SqlDbType.Date)
                    .Value = fecha.Date;
            });
    }

    public async Task<TipoCambioResponse?>
        ObtenerUltimoAsync(
            int idMonedaOrigen,
            int idMonedaDestino)
    {
        return await ObtenerAsync(
            "cfg.sp_TipoCambio_ObtenerUltimo",
            c => AgregarMonedas(
                c,
                idMonedaOrigen,
                idMonedaDestino));
    }

    public async Task<IReadOnlyList<TipoCambioResponse>>
        ListarPorRangoAsync(
            int idMonedaOrigen,
            int idMonedaDestino,
            DateTime fechaDesde,
            DateTime fechaHasta)
    {
        return await ListarAsync(
            "cfg.sp_TipoCambio_ListarPorRango",
            c =>
            {
                AgregarMonedas(
                    c,
                    idMonedaOrigen,
                    idMonedaDestino);

                c.Parameters.Add(
                    "@FechaDesde",
                    SqlDbType.Date)
                    .Value = fechaDesde.Date;

                c.Parameters.Add(
                    "@FechaHasta",
                    SqlDbType.Date)
                    .Value = fechaHasta.Date;
            });
    }

    public Task<TipoCambioResponse>
        CrearAsync(
            TipoCambioCrearRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_TipoCambio_Crear",
            c => AgregarRequest(c, request));
    }

    public Task<TipoCambioResponse>
        GuardarAsync(
            TipoCambioCrearRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_TipoCambio_Guardar",
            c => AgregarRequest(c, request));
    }

    public Task<TipoCambioResponse>
        ActualizarAsync(
            long idTipoCambio,
            TipoCambioActualizarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_TipoCambio_Actualizar",
            c =>
            {
                c.Parameters.Add(
                    "@IdTipoCambio",
                    SqlDbType.BigInt)
                    .Value = idTipoCambio;

                c.Parameters.Add(
                    "@Fecha",
                    SqlDbType.Date)
                    .Value = request.Fecha.Date;

                AgregarDecimal(
                    c,
                    "@ValorCompra",
                    request.ValorCompra);

                AgregarDecimal(
                    c,
                    "@ValorVenta",
                    request.ValorVenta);

                c.Parameters.Add(
                    "@Fuente",
                    SqlDbType.NVarChar,
                    200)
                    .Value =
                    (object?)request.Fuente
                    ?? DBNull.Value;
            });
    }

    private static void AgregarRequest(
        SqlCommand c,
        TipoCambioCrearRequest request)
    {
        AgregarMonedas(
            c,
            request.IdMonedaOrigen,
            request.IdMonedaDestino);

        c.Parameters.Add(
            "@Fecha",
            SqlDbType.Date)
            .Value = request.Fecha.Date;

        AgregarDecimal(
            c,
            "@ValorCompra",
            request.ValorCompra);

        AgregarDecimal(
            c,
            "@ValorVenta",
            request.ValorVenta);

        c.Parameters.Add(
            "@Fuente",
            SqlDbType.NVarChar,
            200)
            .Value =
            (object?)request.Fuente
            ?? DBNull.Value;
    }

    private static void AgregarMonedas(
        SqlCommand c,
        int origen,
        int destino)
    {
        c.Parameters.Add(
            "@IdMonedaOrigen",
            SqlDbType.Int)
            .Value = origen;

        c.Parameters.Add(
            "@IdMonedaDestino",
            SqlDbType.Int)
            .Value = destino;
    }

    private static void AgregarDecimal(
        SqlCommand c,
        string nombre,
        decimal valor)
    {
        var parameter =
            c.Parameters.Add(
                nombre,
                SqlDbType.Decimal);

        parameter.Precision = 18;
        parameter.Scale = 6;
        parameter.Value = valor;
    }

    private async Task<IReadOnlyList<TipoCambioResponse>>
        ListarAsync(
            string procedimiento,
            Action<SqlCommand>? configurar)
    {
        var lista =
            new List<TipoCambioResponse>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        configurar?.Invoke(command);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(Mapear(reader));
        }

        return lista;
    }

    private async Task<TipoCambioResponse?>
        ObtenerAsync(
            string procedimiento,
            Action<SqlCommand> configurar)
    {
        var lista =
            await ListarAsync(
                procedimiento,
                configurar);

        return lista.FirstOrDefault();
    }

    private async Task<TipoCambioResponse>
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

    private static TipoCambioResponse Mapear(
        SqlDataReader r)
    {
        return new TipoCambioResponse
        {
            IdTipoCambio =
                Convert.ToInt64(
                    r["id_tipo_cambio"]),

            IdMonedaOrigen =
                Convert.ToInt32(
                    r["id_moneda_origen"]),

            MonedaOrigenCodigo =
                r["moneda_origen_codigo"]?.ToString()
                ?? string.Empty,

            MonedaOrigenNombre =
                TieneColumna(r, "moneda_origen_nombre")
                    ? r["moneda_origen_nombre"]?.ToString()
                    : null,

            IdMonedaDestino =
                Convert.ToInt32(
                    r["id_moneda_destino"]),

            MonedaDestinoCodigo =
                r["moneda_destino_codigo"]?.ToString()
                ?? string.Empty,

            MonedaDestinoNombre =
                TieneColumna(r, "moneda_destino_nombre")
                    ? r["moneda_destino_nombre"]?.ToString()
                    : null,

            Fecha =
                Convert.ToDateTime(
                    r["fecha"]),

            ValorCompra =
                Convert.ToDecimal(
                    r["valor_compra"]),

            ValorVenta =
                Convert.ToDecimal(
                    r["valor_venta"]),

            Fuente =
                r["fuente"] == DBNull.Value
                    ? null
                    : r["fuente"].ToString()
        };
    }

    private static bool TieneColumna(
        SqlDataReader reader,
        string nombre)
    {
        for (var i = 0;
             i < reader.FieldCount;
             i++)
        {
            if (reader.GetName(i)
                .Equals(
                    nombre,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}