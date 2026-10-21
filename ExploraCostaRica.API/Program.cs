using ExploraCostaRica.Data;
using ExploraCostaRica.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONTROLADORES API
// ============================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ============================================================
// BASE DE DATOS
// ============================================================

builder.Services.AddDbContext<ExploraCostaRicaDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString(
            "ExploraCostaRicaConnection"
        );

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "No se encontró la conexión ExploraCostaRicaConnection."
        );
    }

    options.UseSqlServer(connectionString);
});


// ============================================================
// SERVICIOS NECESARIOS PARA LOS ENDPOINTS
// ============================================================

// Biodiversidad necesita consultar los destinos de la BD.
builder.Services.AddScoped<IDestinationService, DestinationService>();


// ============================================================
// API 1 - UNSPLASH
// ============================================================

builder.Services.AddHttpClient<
    IFotografiasApiService,
    FotografiasApiService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);
    }
);


// ============================================================
// API 2 - GNEWS
// ============================================================

builder.Services.AddHttpClient<
    INoticiasApiService,
    NoticiasApiService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ExploraCostaRica/1.0"
        );
    }
);


// ============================================================
// API 3 - INATURALIST
// ============================================================

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<
    IBiodiversidadApiService,
    BiodiversidadApiService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ExploraCostaRica/1.0"
        );
    }
);


// ============================================================
// CREAR APLICACIÓN
// ============================================================

var app = builder.Build();


// ============================================================
// SWAGGER
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ============================================================
// PIPELINE HTTP
// ============================================================

app.UseHttpsRedirection();

app.UseAuthorization();


// ============================================================
// MAPEAR CONTROLADORES API
// ============================================================

app.MapControllers();


// ============================================================
// EJECUTAR
// ============================================================

app.Run();