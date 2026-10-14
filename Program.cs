
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
// Se encarga de las consultas, generación de JSON,
// registro de descargas y administración del historial.
builder.Services.AddScoped<IDescargasService, DescargasService>();

// Servicio de noticias externas GNews
// Utiliza HttpClient para realizar solicitudes HTTP
// y recibir información JSON desde una API externa.
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
