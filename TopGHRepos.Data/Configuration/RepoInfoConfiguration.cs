using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using TopGHRepos.Models.Entities;

namespace TopGHRepos.Data.Configuration
{
   public class RepoInfoConfiguration : EntityConfiguration<RepositoryInfo>
   {
      public override void Configure(EntityTypeBuilder<RepositoryInfo> builder)
      {
         builder
            .HasIndex(r => r.GitHubId)
            .IsUnique();

         builder
            .Property(r => r.OwnerLoginName)
            .IsRequired();

         builder
            .Property(r => r.Name)
            .IsRequired();
         base.Configure(builder);
      }
   }
}
