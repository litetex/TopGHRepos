using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TopGHRepos.Data.Configuration;
using TopGHRepos.Models.Entities;

namespace TopGHRepos.Data
{
   public class TopGHReposContext : DbContext
   {
      public TopGHReposContext(DbContextOptions<TopGHReposContext> options) : base(options) { }

      public virtual DbSet<RepositoryInfo> RepositoryInfos { get; set; }


      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         modelBuilder.ApplyConfiguration(new RepoInfoConfiguration());
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         optionsBuilder.UseLazyLoadingProxies();

         base.OnConfiguring(optionsBuilder);
      }

      public void DetachAllEntities()
      {
         var changedEntriesCopy = ChangeTracker.Entries()
             .Where(e => e.State != EntityState.Detached)
             .ToList();

         foreach (var entityEntry in changedEntriesCopy)
            entityEntry.State = EntityState.Detached;
      }

      public override int SaveChanges()
      {
         AddAuditValues();

         return base.SaveChanges();
      }

      public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
      {
         AddAuditValues();

         return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
      }

      private void AddAuditValues()
      {
         var entities = ChangeTracker.Entries().Where(x => (x.Entity is Entity)
             && (x.State == EntityState.Added || x.State == EntityState.Detached || x.State == EntityState.Modified));

         foreach (var entityEntry in entities.Where(e => e.Entity is Entity))
         {
            var entity = (Entity)entityEntry.Entity;
            
            if (entityEntry.State == EntityState.Added)
               entity.DateCreated = DateTime.Now;
         }
      }
   }
}
