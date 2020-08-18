using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using TopGHRepos.Models.Entities;

namespace TopGHRepos.Data.Configuration
{
   public abstract class EntityConfiguration<T> : IEntityTypeConfiguration<T>
        where T : Entity
   {
      public virtual void Configure(EntityTypeBuilder<T> builder)
      {
         // Nothing so far
      }

   }

}
