using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SAWSCore3API.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            //Migrations to create identity tables
            //dotnet ef migrations add initPomeloIdentity -c ApplicationDbContext
            //dotnet ef database update

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    if (!optionsBuilder.IsConfigured)
        //    {
        //        optionsBuilder.UseMySql("server=qa.j-cred.co.za;user id=jcredadmin;password=1P@55w0rd;database=rise-mzansi", x => x.ServerVersion("5.7.39-mysql"));
        //    }
        //}

        public DbSet<ApplicationUser> User { get; set; }
    }
}
