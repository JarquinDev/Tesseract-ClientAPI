using Microsoft.EntityFrameworkCore;
using TesseractTest.Model;

namespace TesseractTest.db
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clientes { get; set; }

    }
}
