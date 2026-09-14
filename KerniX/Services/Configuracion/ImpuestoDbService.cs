using System.Data;
using KerniX.Api.Models.Configuracion.Impuesto;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class ImpuestoDbService
    : IImpuestoDbService
{
    private readonly string _connectionString;

    public ImpuestoDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public Task<IReadOnlyList<ImpuestoResponse>>
        ListarAsync(int idEmpresa)
    {
        return ListarEmpresaAsync(
            "cfg.sp_Impuesto_Listar",
            idEmpresa);
    }

    public Task<IReadOnlyList<ImpuestoResponse>>
        ListarActivosAsync(int idEmpresa)
    {
        return ListarEmpresaAsync(
            "cfg.sp_Impuesto_ListarActivos",
            idEmpresa);
    }

    public async Task<IReadOnlyList<ImpuestoResponse>>
        ListarVigentesAsync(
            int idEmpresa,
            DateTime? fecha = null)
    {
        return await ListarAsync(
            "cfg.sp_Impuesto_ListarVigentes",
            c =>
            {
                c.Parameters.Add(
                    "@IdEmpresa",
                    SqlDbType.Int)
                    .Value = idEmpresa;

                c.Parameters.Add(
                    "@Fecha",
                    SqlDbType.Date)
                    .Value =
                    fecha.HasValue
                        ? fecha.Value.Date
                        : DBNull.Value;
            });
    }

    public Task<ImpuestoResponse?>
        ObtenerPorIdAsync(int idImpuesto)
    {
        return ObtenerAsync(
            "cfg.sp_Impuesto_ObtenerPorId",
            c =>
            {
                c.Parameters.Add(
                    "@IdImpuesto",
                    SqlDbType.Int)
                    .Value = idImpuesto;
            });
    }

    public Task<ImpuestoResponse?>
        ObtenerPorCodigoAsync(
            int idEmpresa,
            string codigo)
    {
        return ObtenerAsync(
            "cfg.sp_Impuesto_ObtenerPorCodigo",
            c =>
            {
                c.Parameters.Add(
                    "@IdEmpresa",
                    SqlDbType.Int)
                    .Value = idEmpresa;

                c.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    60)
                    .Value = codigo;
            });
    }

    public Task<ImpuestoResponse?>
        ObtenerVigenteAsync(
            int idEmpresa,
            string codigo)
    {
        return ObtenerAsync(
            "cfg.sp_Impuesto_ObtenerVigente",
            c =>
            {
                c.Parameters.Add(
                    "@IdEmpresa",
                    SqlDbType.Int)
                    .Value = idEmpresa;

                c.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    60)
                    .Value = codigo;
            });
    }

    public Task<ImpuestoResponse>
        CrearAsync(
            ImpuestoCrearRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_Impuesto_Crear",
            c =>
            {
                c.Parameters.Add(
                    "@IdEmpresa",
                    SqlDbType.Int)
                    .Value = request.IdEmpresa;

                c.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    60)
                    .Value = request.Codigo;

                c.Parameters.Add(
                    "@Nombre",
                    SqlDbType.NVarChar,
                    200)
                    .Value = request.Nombre;

                AgregarPorcentaje(
                    c,
                    request.Porcentaje);

                AgregarFechas(
                    c,
                    request.FechaInicio,
                    request.FechaFin);

                c.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = request.Estado;
            });
    }

    public Task<ImpuestoResponse>
        ActualizarAsync(
            int idImpuesto,
            ImpuestoActualizarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_Impuesto_Actualizar",
            c =>
            {
                c.Parameters.Add(
                    "@IdImpuesto",
                    SqlDbType.Int)
                    .Value = idImpuesto;

                c.Parameters.Add(
                    "@Nombre",
                    SqlDbType.NVarChar,
                    200)
                    .Value = request.Nombre;

                AgregarPorcentaje(
                    c,
                    request.Porcentaje);

                AgregarFechas(
                    c,
                    request.FechaInicio,
                    request.FechaFin);

                c.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = request.Estado;
            });
    }

    public Task<ImpuestoResponse>
        CambiarEstadoAsync(
            int idImpuesto,
            bool estado)
    {
        return EjecutarAsync(
            "cfg.sp_Impuesto_CambiarEstado",
            c =>
            {
                c.Parameters.Add(
                    "@IdImpuesto",
                    SqlDbType.Int)
                    .Value = idImpuesto;

                c.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = estado;
            });
    }

    public Task<ImpuestoResponse>
        CerrarVigenciaAsync(
            int idImpuesto,
            DateTime fechaFin)
    {
        return EjecutarAsync(
            "cfg.sp_Impuesto_CerrarVigencia",
            c =>
            {
                c.Parameters.Add(
                    "@IdImpuesto",
                    SqlDbType.Int)
                    .Value = idImpuesto;

                c.Parameters.Add(
                    "@FechaFin",
                    SqlDbType.Date)
                    .Value = fechaFin.Date;
            });
    }

    private Task<IReadOnlyList<ImpuestoResponse>>
        ListarEmpresaAsync(
            string procedimiento,
            int idEmpresa)
    {
        return ListarAsync(
            procedimiento,
            c =>
            {
                c.Parameters.Add(
                    "@IdEmpresa",
                    SqlDbType.Int)
                    .Value = idEmpresa;
            });
    }

    private async Task<IReadOnlyList<ImpuestoResponse>>
        ListarAsync(
            string procedimiento,
            Action<SqlCommand>? configurar)
    {
        var lista =
            new List<ImpuestoResponse>();

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
            lista.Add(Mapear(reader));

        return lista;
    }

    private async Task<ImpuestoResponse?>
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

    private async Task<ImpuestoResponse>
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

    private static void AgregarPorcentaje(
        SqlCommand c,
        decimal porcentaje)
    {
        var p =
            c.Parameters.Add(
                "@Porcentaje",
                SqlDbType.Decimal);

        p.Precision = 9;
        p.Scale = 4;
        p.Value = porcentaje;
    }

    private static void AgregarFechas(
        SqlCommand c,
        DateTime inicio,
        DateTime? fin)
    {
        c.Parameters.Add(
            "@FechaInicio",
            SqlDbType.Date)
            .Value = inicio.Date;

        c.Parameters.Add(
            "@FechaFin",
            SqlDbType.Date)
            .Value =
            fin.HasValue
                ? fin.Value.Date
                : DBNull.Value;
    }

    private static ImpuestoResponse Mapear(
        SqlDataReader r)
    {
        return new ImpuestoResponse
        {
            IdImpuesto =
                Convert.ToInt32(
                    r["id_impuesto"]),

            IdEmpresa =
                Convert.ToInt32(
                    r["id_empresa"]),

            Codigo =
                r["codigo"]?.ToString()
                ?? string.Empty,

            Nombre =
                r["nombre"]?.ToString()
                ?? string.Empty,

            Porcentaje =
                Convert.ToDecimal(
                    r["porcentaje"]),

            FechaInicio =
                Convert.ToDateTime(
                    r["fecha_inicio"]),

            FechaFin =
                r["fecha_fin"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        r["fecha_fin"]),

            Estado =
                Convert.ToBoolean(
                    r["estado"])
        };
    }
}