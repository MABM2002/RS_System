using Microsoft.EntityFrameworkCore;
using Rs_system.Models;

namespace Rs_system.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Persona> Personas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<RolSistema> RolesSistema { get; set; }
    public DbSet<RolUsuario> RolesUsuario { get; set; }
    public DbSet<Permiso> Permisos { get; set; }
    public DbSet<Modulo> Modulos { get; set; }
    public DbSet<RolPermiso> RolesPermisos { get; set; }
    
    public DbSet<ConfiguracionSistema> Configuraciones { get; set; }
    
    public DbSet<AsistenciaCulto> AsistenciasCulto { get; set; }
    
    
    // Offerings module
    public DbSet<RegistroCulto> RegistrosCulto { get; set; }
    public DbSet<Ofrenda> Ofrendas { get; set; }
    public DbSet<DescuentoOfrenda> DescuentosOfrenda { get; set; }
    
    // Church Members module
    public DbSet<GrupoTrabajo> GruposTrabajo { get; set; }
    public DbSet<Miembro> Miembros { get; set; }
    
    // Inventory module
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<EstadoArticulo> EstadosArticulos { get; set; }
    public DbSet<Ubicacion> Ubicaciones { get; set; }
    public DbSet<Existencia> Existencias { get; set; }
    public DbSet<Articulo> Articulos { get; set; }
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure composite key for RolUsuario
        modelBuilder.Entity<RolUsuario>()
            .HasKey(ru => new { ru.UsuarioId, ru.RolId });
        
        // Configure relationships
        modelBuilder.Entity<RolUsuario>()
            .HasOne(ru => ru.Usuario)
            .WithMany(u => u.RolesUsuario)
            .HasForeignKey(ru => ru.UsuarioId);
        
        modelBuilder.Entity<RolUsuario>()
            .HasOne(ru => ru.Rol)
            .WithMany(r => r.RolesUsuario)
            .HasForeignKey(ru => ru.RolId);

        // Configure composite key for RolPermiso
        modelBuilder.Entity<RolPermiso>()
            .HasKey(rp => new { rp.RolId, rp.PermisoId });

        modelBuilder.Entity<RolPermiso>()
            .HasOne(rp => rp.Rol)
            .WithMany(r => r.RolesPermisos)
            .HasForeignKey(rp => rp.RolId);

        modelBuilder.Entity<RolPermiso>()
            .HasOne(rp => rp.Permiso)
            .WithMany()
            .HasForeignKey(rp => rp.PermisoId);

        modelBuilder.Entity<Permiso>()
            .HasOne(p => p.Modulo)
            .WithMany(m => m.Permisos)
            .HasForeignKey(p => p.ModuloId);
        
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Persona)
            .WithMany()
            .HasForeignKey(u => u.PersonaId);
        
        // Church Members module relationships
        modelBuilder.Entity<Miembro>()
            .HasOne(m => m.GrupoTrabajo)
            .WithMany(g => g.Miembros)
            .HasForeignKey(m => m.GrupoTrabajoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Miembro>()
            .HasOne(m => m.Persona)
            .WithMany()
            .HasForeignKey(m => m.PersonaId)
            .OnDelete(DeleteBehavior.Restrict);


        // Global configuration: Convert all dates to UTC when saving
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in properties)
            {
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    v => v));
            }
        }
    }
}