using Microsoft.EntityFrameworkCore;
using WebAPIExamen.Models; 

namespace WebAPIExamen.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Paciente> pacientes_13449 { get; set; }
    }
}