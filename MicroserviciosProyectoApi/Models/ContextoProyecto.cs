using Microsoft.EntityFrameworkCore;

namespace Proyecto.Models;

public class ContextoProyecto : DbContext
{
    public ContextoProyecto(DbContextOptions<ContextoProyecto> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<TokenSesion> TokensSesion => Set<TokenSesion>();

    public DbSet<Directorio> Directorios => Set<Directorio>();

    public DbSet<Archivo> Archivos => Set<Archivo>();

    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<ContenidoCompartido> ContenidosCompartidos => Set<ContenidoCompartido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {

            entity.ToTable("Usuarios");

            entity.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Correo).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Contrasena).HasMaxLength(255).IsRequired();

            entity.Property(u => u.SelloSesion).HasMaxLength(36).IsRequired();

            entity.Property(u => u.FechaNacimiento)
                .HasConversion(
                    fecha => fecha.ToDateTime(TimeOnly.MinValue),
                    fecha => DateOnly.FromDateTime(fecha))
                .HasColumnType("date");

            entity.HasIndex(u => u.Correo).IsUnique();
        });

        modelBuilder.Entity<TokenSesion>(entity =>
        {
            entity.ToTable("TokensSesion");

            entity.Property(t => t.Token).HasMaxLength(64).IsRequired();

            entity.HasIndex(t => t.Token).IsUnique();

            entity.HasIndex(t => t.UsuarioId).IsUnique();

            entity.HasOne(t => t.Usuario)
                .WithOne(u => u.TokenSesion)
                .HasForeignKey<TokenSesion>(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Directorio>(entity =>
        {
            entity.ToTable("Directorios");

            entity.Property(d => d.Nombre).HasMaxLength(200).IsRequired();

            entity.HasIndex(d => new { d.EquipoId, d.PadreId, d.Nombre }).IsUnique();

            entity.HasOne(d => d.Padre)
                .WithMany(d => d.Subdirectorios)
                .HasForeignKey(d => d.PadreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Propietario)
                .WithMany()
                .HasForeignKey(d => d.PropietarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Archivo>(entity =>
        {
            entity.ToTable("Archivos");

            entity.Property(a => a.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Extension).HasMaxLength(20).IsRequired();
            entity.Property(a => a.NombreAlmacenamiento).HasMaxLength(80).IsRequired();
            entity.Property(a => a.TipoContenido).HasMaxLength(150).IsRequired();

            entity.HasIndex(a => a.NombreAlmacenamiento).IsUnique();

            entity.HasIndex(a => new { a.DirectorioId, a.Nombre });

            entity.HasOne(a => a.Directorio)
                .WithMany(d => d.Archivos)
                .HasForeignKey(a => a.DirectorioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Propietario)
                .WithMany()
                .HasForeignKey(a => a.PropietarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.ToTable("Equipos");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Nombre).IsUnique();
        });
        modelBuilder.Entity<Usuario>().HasOne(u => u.Equipo).WithMany()
            .HasForeignKey(u => u.EquipoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Directorio>().HasOne(d => d.Equipo).WithMany()
            .HasForeignKey(d => d.EquipoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContenidoCompartido>(entity =>
        {
            entity.ToTable("ContenidosCompartidos", t => t.HasCheckConstraint(
                "CK_Compartido_UnRecurso", "(`ArchivoId` IS NULL) <> (`DirectorioId` IS NULL)"));
            entity.HasOne(c => c.EquipoDestino).WithMany().HasForeignKey(c => c.EquipoDestinoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Archivo).WithMany().HasForeignKey(c => c.ArchivoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Directorio).WithMany().HasForeignKey(c => c.DirectorioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(c => new { c.EquipoDestinoId, c.ArchivoId }).IsUnique();
            entity.HasIndex(c => new { c.EquipoDestinoId, c.DirectorioId }).IsUnique();
        });
    }
}
