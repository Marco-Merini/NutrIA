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
    }
}
