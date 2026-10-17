using ExploraCostaRica.Data;
using ExploraCostaRica.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICIOS MVC
// ============================================================

builder.Services.AddControllersWithViews();


// ============================================================
// CONEXIÓN A SQL SERVER
// ============================================================

builder.Services.AddDbContext<ExploraCostaRicaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "ExploraCostaRicaConnection"
        )
    )
);


// ============================================================
// SERVICIOS DE LA APLICACIÓN
// ============================================================

// Servicio de Explorar y Búsqueda
builder.Services.AddScoped<IExplorarService, ExplorarService>();

// Servicio de Descargas
// Consultas, generación de JSON, registro de descargas
// y administración del historial.
builder.Services.AddScoped<IDescargasService, DescargasService>();

// Servicio de Destinos y Sorpréndeme
// Lista de destinos, detalle y selección aleatoria real.
builder.Services.AddScoped<IDestinationService, DestinationService>();

// ============================================================
// API EXTERNA: GNEWS
// ============================================================

builder.Services.AddHttpClient<INoticiasApiService, NoticiasApiService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ExploraCostaRica/1.0"
        );
    }
);


// ============================================================
// API EXTERNA: UNSPLASH (FOTOGRAFÍAS)
// ============================================================

builder.Services.AddHttpClient<IFotografiasApiService, FotografiasApiService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);
    }
);


// ============================================================
// CREAR LA APLICACIÓN
// ============================================================

var app = builder.Build();


// ============================================================
// CONFIGURACIÓN DEL PIPELINE HTTP
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// ============================================================
// HTTPS
// ============================================================

app.UseHttpsRedirection();


// ============================================================
// ARCHIVOS ESTÁTICOS
// ============================================================

app.UseStaticFiles();


// ============================================================
// ENRUTAMIENTO
// ============================================================

app.UseRouting();


// ============================================================
// AUTORIZACIÓN
// ============================================================

app.UseAuthorization();


// ============================================================
// RUTA MVC PRINCIPAL
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// ============================================================
// EJECUTAR LA APLICACIÓN
// ============================================================

app.Run();