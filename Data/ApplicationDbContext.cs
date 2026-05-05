using Microsoft.EntityFrameworkCore;
using NutriFlow.Models;

namespace NutriFlow.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<PlanoDieta> PlanosDieta { get; set; }
        public DbSet<Refeicao> Refeicoes { get; set; }
        public DbSet<Sessao> Sessoes { get; set; }
        public DbSet<Progresso> Progressos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Paciente
            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.Property(e => e.Altura).HasPrecision(5, 2);
                entity.Property(e => e.PesoAtual).HasPrecision(5, 2);
            });

            // PlanoDieta
            modelBuilder.Entity<PlanoDieta>(entity =>
            {
                entity.Property(e => e.CaloriasDiarias).HasPrecision(8, 2);
                entity.Property(e => e.ProteinasG).HasPrecision(8, 2);
                entity.Property(e => e.CarboidratosG).HasPrecision(8, 2);
                entity.Property(e => e.GordurasG).HasPrecision(8, 2);
            });

            // Progresso
            modelBuilder.Entity<Progresso>(entity =>
            {
                entity.Property(e => e.Peso).HasPrecision(5, 2);
                entity.Property(e => e.CinturaCm).HasPrecision(5, 2);
                entity.Property(e => e.QuadrilCm).HasPrecision(5, 2);
                entity.Property(e => e.PercentualGordura).HasPrecision(5, 2);
            });

            // Refeicao
            modelBuilder.Entity<Refeicao>(entity =>
            {
                entity.Property(e => e.Calorias).HasPrecision(8, 2);
            });

            // Sessao
            modelBuilder.Entity<Sessao>(entity =>
            {
                entity.Property(e => e.PesoSessao).HasPrecision(5, 2);
            });
        }
    }
}
