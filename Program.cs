using ExploraCostaRica.Data;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICIOS MVC
// ============================================================

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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

// Servicio de Favoritos
// Agrega, elimina, consulta y administra los favoritos del usuario.
builder.Services.AddScoped<IFavoritosService, FavoritosService>();


builder.Services.AddScoped<IAuthService, AuthService>();

// Servicios del Dashboard (formato JSON común v1.0)
// Dashboard: consulta, importa y exporta Sources / SourceItems.
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Sincronizar: convierte GNews, Unsplash e iNaturalist al formato común.
builder.Services.AddScoped<IDashboardSyncService, DashboardSyncService>();


// ============================================================
// AUTENTICACIÓN POR COOKIE (tabla Users)
// ============================================================

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "ExploraCostaRica.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;


        options.Events.OnRedirectToLogin = context =>
        {
            var ruta = context.Request.Path;

            bool esAjax =
                ruta.StartsWithSegments("/Favoritos/Ids") ||
                (HttpMethods.IsPost(context.Request.Method) &&
                 ruta.StartsWithSegments("/Favoritos"));

            if (esAjax)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });


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
// API EXTERNA: INATURALIST (BIODIVERSIDAD)
// ============================================================

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IBiodiversidadApiService, BiodiversidadApiService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ExploraCostaRica/1.0"
        );
    }
);


// ============================================================
// API EXTERNA: WIKIMEDIA COMMONS (CULTURA)
// ============================================================

builder.Services.AddHttpClient<IWikimediaApiService, WikimediaApiService>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ExploraCostaRica/1.0 (proyecto academico SC-701)"
        );
    }
);


// ============================================================
// CREAR LA APLICACIÓN
// ============================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


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
// AUTENTICACIÓN Y AUTORIZACIÓN
// (Authentication siempre antes de Authorization)
// ============================================================

app.UseAuthentication();

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