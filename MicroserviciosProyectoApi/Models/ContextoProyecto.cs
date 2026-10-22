using Microsoft.EntityFrameworkCore;

namespace Proyecto.Models;

/// <summary>
/// Contexto de datos del proyecto. Es la única pieza que traduce las clases de C# a
/// tablas y columnas de MySQL, y concentra la configuración de índices y relaciones.
/// </summary>
/// <remarks>
/// El nombre de la clase también está en español: antes se llamaba <c>ProyectoContext</c>
/// y ahora es <c>ContextoProyecto</c>, que es el nombre que aparece en las migraciones y
/// en el archivo de instantánea del modelo.
/// </remarks>
public class ContextoProyecto : DbContext
{
    public ContextoProyecto(DbContextOptions<ContextoProyecto> options)
        : base(options)
    {
    }

    /// <summary>Cuentas del sistema. Tabla "Usuarios".</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Tokens de refresco vigentes. Tabla "TokensSesion".</summary>
    public DbSet<TokenSesion> TokensSesion => Set<TokenSesion>();

    /// <summary>Directorios del repositorio. Tabla "Directorios" (módulo 3.1).</summary>
    public DbSet<Directorio> Directorios => Set<Directorio>();

    /// <summary>Archivos subidos. Tabla "Archivos" (módulo 3.2).</summary>
    public DbSet<Archivo> Archivos => Set<Archivo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            // El nombre de la tabla se declara explícitamente para que quede claro en las
            // migraciones; sin esto EF tomaría el nombre desde la propiedad DbSet.
            entity.ToTable("Usuarios");

            entity.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Correo).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Contrasena).HasMaxLength(255).IsRequired();

            // El proveedor MySql.EntityFrameworkCore devuelve la columna DATE como DateTime,
            // y al materializar el usuario intenta convertirla a DateOnly y falla en tiempo
            // de ejecución. Con este convertidor se le indica cómo pasar de una a otra:
            // la columna sigue siendo DATE en la base de datos y DateOnly en C#.
            entity.Property(u => u.FechaNacimiento)
                .HasConversion(
                    fecha => fecha.ToDateTime(TimeOnly.MinValue),
                    fecha => DateOnly.FromDateTime(fecha))
                .HasColumnType("date");

            // No puede haber dos usuarios registrados con el mismo correo.
            entity.HasIndex(u => u.Correo).IsUnique();
        });

        modelBuilder.Entity<TokenSesion>(entity =>
        {
            entity.ToTable("TokensSesion");

            // En esta columna no se guarda el token de refresco, sino su hash SHA-256 en
            // hexadecimal (siempre 64 caracteres), por eso el tamaño es fijo y corto.
            entity.Property(t => t.Token).HasMaxLength(64).IsRequired();

            entity.HasIndex(t => t.Token).IsUnique();

            // Un solo token vigente por usuario (índice único sobre la llave foránea).
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

            // No puede haber dos directorios con el mismo nombre en el mismo nivel.
            // Ojo: MySQL permite varios NULL en un índice único, así que este índice cubre
            // los subdirectorios y la unicidad en la raíz (PadreId nulo) se valida en el
            // controlador antes de insertar.
            entity.HasIndex(d => new { d.PadreId, d.Nombre }).IsUnique();

            // La relación del directorio consigo mismo se configura como Restrict y no como
            // Cascade porque MySQL no admite el borrado en cascada sobre una llave foránea
            // autorreferente. El borrado recursivo (administrador sobre un directorio con
            // contenido) se hace explícitamente en el código, nivel por nivel, desde el más
            // profundo hacia arriba.
            entity.HasOne(d => d.Padre)
                .WithMany(d => d.Subdirectorios)
                .HasForeignKey(d => d.PadreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Si se elimina el usuario creador, sus directorios se eliminan con él.
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

            // El nombre físico es único por diseño (se genera con un GUID).
            entity.HasIndex(a => a.NombreAlmacenamiento).IsUnique();

            // Acelera la consulta por directorio, que es la forma principal de listar archivos.
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
    }
}
