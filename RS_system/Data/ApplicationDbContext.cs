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
    public DbSet<ContabilidadRegistro> ContabilidadRegistros { get; set; }
    public DbSet<ReporteMensualContable> ReportesMensualesContables { get; set; }

    // General church accounting module
    public DbSet<CategoriaIngreso> CategoriasIngreso { get; set; }
    public DbSet<CategoriaEgreso> CategoriasEgreso { get; set; }
    public DbSet<MovimientoGeneral> MovimientosGenerales { get; set; }
    public DbSet<MovimientoGeneralAdjunto> MovimientosGeneralesAdjuntos { get; set; }
    public DbSet<ReporteMensualGeneral> ReportesMensualesGenerales { get; set; }
    
    // Inventory module
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<EstadoArticulo> EstadosArticulos { get; set; }
    public DbSet<Ubicacion> Ubicaciones { get; set; }
    public DbSet<Existencia> Existencias { get; set; }
    public DbSet<Articulo> Articulos { get; set; }
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
    public DbSet<Prestamo> Prestamos { get; set; }
    public DbSet<PrestamoDetalle> PrestamoDetalles { get; set; }
    
    // Collaborations module
    public DbSet<TipoColaboracion> TiposColaboracion { get; set; }
    public DbSet<Colaboracion> Colaboraciones { get; set; }
    public DbSet<DetalleColaboracion> DetalleColaboraciones { get; set; }

    // Diezmos module
    public DbSet<DiezmoCierre>      DiezmoCierres      { get; set; }
    public DbSet<DiezmoDetalle>     DiezmoDetalles     { get; set; }
    public DbSet<DiezmoSalida>      DiezmoSalidas      { get; set; }
    public DbSet<DiezmoBeneficiario> DiezmoBeneficiarios { get; set; }
    public DbSet<DiezmoTipoSalida>  DiezmoTiposSalida  { get; set; }

    // Dynamic Documents module
    public DbSet<Documento> Documentos { get; set; }
    public DbSet<DocumentoDetalle> DocumentoDetalles { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        // 1. Registrar el enum de PostgreSQL
        modelBuilder.HasPostgresEnum<TipoMovimientoGeneral>("tipo_movimiento_general");

        // 2. Asegurar que la propiedad use ese tipo
        modelBuilder.Entity<MovimientoGeneral>()
            .Property(e => e.Tipo)
            .HasColumnType("tipo_movimiento_general");
        base.OnModelCreating(modelBuilder);
        
        // Configure composite key for RolUsuario
        modelBuilder.Entity<RolUsuario>()
            .HasKey(ru => new { ru.UsuarioId, ru.RolId });

        modelBuilder.Entity<Documento>().HasKey(e =>  e.IdDocumento);
        modelBuilder.Entity<DocumentoDetalle>().HasKey(e =>  e.IdDetalle);
        
        
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

        modelBuilder.Entity<ContabilidadRegistro>()
            .HasOne(c => c.GrupoTrabajo)
            .WithMany()
            .HasForeignKey(c => c.GrupoTrabajoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReporteMensualContable>()
            .HasOne(r => r.GrupoTrabajo)
            .WithMany()
            .HasForeignKey(r => r.GrupoTrabajoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReporteMensualContable>()
            .HasMany(r => r.Registros)
            .WithOne(c => c.ReporteMensual)
            .HasForeignKey(c => c.ReporteMensualId)
            .OnDelete(DeleteBehavior.Cascade);

        // General accounting module relationships
        modelBuilder.Entity<ReporteMensualGeneral>()
            .HasMany(r => r.Movimientos)
            .WithOne(m => m.ReporteMensualGeneral)
            .HasForeignKey(m => m.ReporteMensualGeneralId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReporteMensualGeneral>()
            .HasIndex(r => new { r.Mes, r.Anio })
            .IsUnique();

        modelBuilder.Entity<MovimientoGeneral>()
            .HasOne(m => m.CategoriaIngreso)
            .WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.CategoriaIngresoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MovimientoGeneral>()
            .HasOne(m => m.CategoriaEgreso)
            .WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.CategoriaEgresoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MovimientoGeneral>()
            .HasMany(m => m.Adjuntos)
            .WithOne(a => a.MovimientoGeneral)
            .HasForeignKey(a => a.MovimientoGeneralId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.HasPostgresEnum<TipoMovimiento>("tipo_movimiento");

        modelBuilder.Entity<MovimientoInventario>()
            .Property(e => e.TipoMovimiento)
            .HasColumnType("tipo_movimiento");
        
        // Collaborations module configuration
        modelBuilder.Entity<TipoColaboracion>(entity =>
        {
            entity.ToTable("tipos_colaboracion", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MontoSugerido).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Colaboracion>(entity =>
        {
            entity.ToTable("colaboraciones", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(10,2)");
            
            entity.HasOne(e => e.Miembro)
                .WithMany()
                .HasForeignKey(e => e.MiembroId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasMany(e => e.Detalles)
                .WithOne(d => d.Colaboracion)
                .HasForeignKey(d => d.ColaboracionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DetalleColaboracion>(entity =>
        {
            entity.ToTable("detalle_colaboraciones", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasColumnType("decimal(10,2)");
            
            entity.HasOne(e => e.TipoColaboracion)
                .WithMany(t => t.Detalles)
                .HasForeignKey(e => e.TipoColaboracionId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => new { e.ColaboracionId, e.TipoColaboracionId, e.Mes, e.Anio })
                .IsUnique();
        });

        // ── Diezmos module configuration ──────────────────────────────────
        modelBuilder.Entity<DiezmoCierre>(entity =>
        {
            entity.ToTable("diezmo_cierres", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalRecibido).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalCambio).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalNeto).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalSalidas).HasColumnType("numeric(12,2)");
            entity.Property(e => e.SaldoFinal).HasColumnType("numeric(12,2)");

            // One closure per date
            entity.HasIndex(e => e.Fecha).IsUnique();

            entity.HasMany(e => e.Detalles)
                .WithOne(d => d.DiezmoCierre)
                .HasForeignKey(d => d.DiezmoCierreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Salidas)
                .WithOne(s => s.DiezmoCierre)
                .HasForeignKey(s => s.DiezmoCierreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiezmoDetalle>(entity =>
        {
            entity.ToTable("diezmo_detalles", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MontoEntregado).HasColumnType("numeric(12,2)");
            entity.Property(e => e.CambioEntregado).HasColumnType("numeric(12,2)");
            entity.Property(e => e.MontoNeto).HasColumnType("numeric(12,2)");

            entity.HasIndex(e => e.DiezmoCierreId);
            entity.HasIndex(e => e.MiembroId);

            entity.HasOne(e => e.Miembro)
                .WithMany()
                .HasForeignKey(e => e.MiembroId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiezmoSalida>(entity =>
        {
            entity.ToTable("diezmo_salidas", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasColumnType("numeric(12,2)");

            entity.HasIndex(e => e.DiezmoCierreId);

            entity.HasOne(e => e.TipoSalida)
                .WithMany(t => t.Salidas)
                .HasForeignKey(e => e.TipoSalidaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Beneficiario)
                .WithMany(b => b.Salidas)
                .HasForeignKey(e => e.BeneficiarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiezmoTipoSalida>(entity =>
        {
            entity.ToTable("diezmo_tipos_salida", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<DiezmoBeneficiario>(entity =>
        {
            entity.ToTable("diezmo_beneficiarios", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        });

        
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