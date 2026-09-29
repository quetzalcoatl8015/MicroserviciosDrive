using Microsoft.EntityFrameworkCore;

namespace Proyecto.Models;

public class ProyectoContext : DbContext
{
    public ProyectoContext(DbContextOptions<ProyectoContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
}