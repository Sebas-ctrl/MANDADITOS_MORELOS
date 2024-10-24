using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stripe;
using System.Text.Json;

namespace MANDADITOS_MORELOS.Models;

public class MorelosContext : DbContext
{
    public MorelosContext(DbContextOptions<MorelosContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PedidosModel>(entity =>
        {
            entity.Property(e => e.LugarOrigen)
                  .HasColumnType("json");

            entity.Property(e => e.LugarDestino)
                  .HasColumnType("json");
        });
    }

    public DbSet<PersonasModel> Personas { get; set; } = null!;
    public DbSet<ClientesModel> Clientes { get; set; } = null!;
    public DbSet<ChoferesModel> Choferes { get; set; } = null!;
    public DbSet<PagosModel> Pagos { get; set; } = null!;
    public DbSet<UnidadesModel> Unidades { get; set; } = null!;
    public DbSet<ValoracionesModel> Valoraciones { get; set; } = null!;
    public DbSet<PedidosModel> Pedidos { get; set; } = null!;
}