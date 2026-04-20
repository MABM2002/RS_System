using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Services;
using RS_system.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") ??
                       throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<Rs_system.Models.TipoMovimiento>("tipo_movimiento_new");

var dataSource = dataSourceBuilder.Build();
dataSourceBuilder.EnableUnmappedTypes();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.MapEnum<TipoMovimientoGeneral>("tipo_movimiento_general");
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    })
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IMiembroService, MiembroService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IEstadoArticuloService, EstadoArticuloService>();
builder.Services.AddScoped<IUbicacionService, UbicacionService>();
builder.Services.AddScoped<IArticuloService, ArticuloService>();
builder.Services.AddScoped<IMovimientoService, MovimientoService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IContabilidadService, ContabilidadService>();
builder.Services.AddScoped<IContabilidadGeneralService, ContabilidadGeneralService>();
builder.Services.AddScoped<IPrestamoService, PrestamoService>();
builder.Services.AddScoped<IColaboracionService, ColaboracionService>();
builder.Services.AddSingleton<IQueryCacheService, QueryCacheService>();

// PostgreSQL direct executor (para procedimientos almacenados y consultas directas)
builder.Services.AddScoped<IPostgresDirectExecutor, PostgresDirectExecutor>();

// Diezmos module services
builder.Services.AddScoped<IDiezmoCalculoService, DiezmoCalculoService>();
builder.Services.AddScoped<IDiezmoCierreService,  DiezmoCierreService>();
builder.Services.AddScoped<IDiezmoReciboService,  DiezmoReciboService>();

// Plantillas de Documentos dinámicos
builder.Services.AddScoped<IGeneradorDocumentoService, GeneradorDocumentoService>();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // 1024 cache entries max
    options.CompactionPercentage = 0.25; // Compact when 25% of entries are expired
});

// Configure cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "RS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddControllersWithViews(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
    options.Filters.Add(new Rs_system.Filters.DynamicAuthorizationFilter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
