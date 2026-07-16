using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Expediente> Expedientes => Set<Expediente>();
    public DbSet<Pagare> Pagares => Set<Pagare>();
    public DbSet<Deudor> Deudores => Set<Deudor>();
    public DbSet<SubPagare> SubPagares => Set<SubPagare>();
    public DbSet<PagareDeudor> PagareDeudores => Set<PagareDeudor>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Cliente 1:N Expediente
        builder.Entity<Expediente>()
            .HasOne(e => e.Cliente)
            .WithMany(c => c.Expedientes)
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Expediente 1:N Pagare
        builder.Entity<Pagare>()
            .HasOne(p => p.Expediente)
            .WithMany(e => e.Pagares)
            .HasForeignKey(p => p.ExpedienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Pagare 1:N SubPagare (cascade)
        builder.Entity<SubPagare>()
            .HasOne(sp => sp.Pagare)
            .WithMany(p => p.SubPagares)
            .HasForeignKey(sp => sp.PagareId)
            .OnDelete(DeleteBehavior.Cascade);

        // Pagare N:M Deudor via PagareDeudor
        builder.Entity<PagareDeudor>()
            .HasKey(pd => new { pd.PagareId, pd.DeudorId });

        builder.Entity<PagareDeudor>()
            .HasOne(pd => pd.Pagare)
            .WithMany(p => p.PagareDeudores)
            .HasForeignKey(pd => pd.PagareId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PagareDeudor>()
            .HasOne(pd => pd.Deudor)
            .WithMany(d => d.PagareDeudores)
            .HasForeignKey(pd => pd.DeudorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Pagare -> Usuario (creador)
        builder.Entity<Pagare>()
            .HasOne(p => p.Usuario)
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
