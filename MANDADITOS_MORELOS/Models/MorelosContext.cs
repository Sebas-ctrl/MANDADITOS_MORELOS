using Microsoft.EntityFrameworkCore;

namespace MANDADITOS_MORELOS.Models;

public class MorelosContext : DbContext
{
    public MorelosContext(DbContextOptions<MorelosContext> options)
    : base(options)
    {
    }

    public DbSet<PersonasModel> Personas { get; set; } = null!;
    public DbSet<UsuariosModel> Usuarios { get; set; } = null!;
}