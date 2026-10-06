using ExploraCostaRica.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICIOS MVC
// ============================================================

// Agrega soporte para Controllers y Views
builder.Services.AddControllersWithViews();


// ============================================================
// CONEXIÓN A LA BASE DE DATOS
// ============================================================

// Registra el DbContext generado por Entity Framework.
//
// La cadena de conexión se encuentra en appsettings.json
// bajo el nombre:
// ExploraCostaRicaConnection
//
// Con esto la aplicación podrá trabajar con:
// ExploraCostaRicaDB
builder.Services.AddDbContext<ExploraCostaRicaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "ExploraCostaRicaConnection"
        )
    )
);


// ============================================================
// CREAR LA APLICACIÓN
// ============================================================

var app = builder.Build();


// ============================================================
// CONFIGURACIÓN DEL PIPELINE HTTP
// ============================================================

// Si la aplicación NO está en ambiente de desarrollo,
// utiliza la página general de errores.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // Activa HSTS para mejorar seguridad HTTPS.
    app.UseHsts();
}


// ============================================================
// HTTPS
// ============================================================

// Redirige automáticamente de HTTP a HTTPS.
app.UseHttpsRedirection();


// ============================================================
// ARCHIVOS ESTÁTICOS
// ============================================================

// Permite utilizar archivos dentro de wwwroot.
//
// Ejemplos:
// - CSS
// - JavaScript
// - imágenes
// - Bootstrap
app.UseStaticFiles();


// ============================================================
// ROUTING
// ============================================================

// Activa el sistema de rutas de ASP.NET Core.
app.UseRouting();


// ============================================================
// AUTORIZACIÓN
// ============================================================

// Más adelante podremos agregar autenticación y usuarios.
// Por ahora dejamos habilitado el middleware de autorización.
app.UseAuthorization();


// ============================================================
// RUTA MVC PRINCIPAL
// ============================================================

// Define la ruta predeterminada.
//
// Si una persona entra simplemente a:
// https://localhost:xxxx/
//
// ASP.NET abrirá:
//
// HomeController
//      ↓
// Index()
//      ↓
// Views/Home/Index.cshtml
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// ============================================================
// EJECUTAR LA APLICACIÓN
// ============================================================

app.Run();