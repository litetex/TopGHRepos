using System;
using System.Collections.Generic;
using System.Text;

namespace TopGHRepos.Models.Entities
{
   public abstract class Entity
   {
      public long Id { get; set; }
      public DateTime DateCreated { get; set; }
   }
}
