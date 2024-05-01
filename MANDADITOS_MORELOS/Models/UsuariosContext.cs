using Microsoft.EntityFrameworkCore;

namespace MANDADITOS_MORELOS.Models;

public class UsuariosContext : DbContext
{
    public UsuariosContext(DbContextOptions<UsuariosContext> options)
    : base(options)
    {
    }

    public DbSet<UsuariosModel> Usuarios { get; set; } = null!;
}