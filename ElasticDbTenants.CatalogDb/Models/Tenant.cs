using System;
using System.ComponentModel.DataAnnotations;

namespace ElasticDbTenants.CatalogDb.Models
{
    public class Tenant
    {
        [Key]
        public Guid Id { get; set; }
        [Required, MaxLength(128)]
        public string Name { get; set; }
        public TenantCreationStatus CreationStatus { get; set; }
        [Required, MaxLength(512)]
        public string ConnectionString { get; set; }
        [Required, MaxLength(128)]
        public string ServerName { get; set; }
        [Required, MaxLength(128)]
        public string DatabaseName { get; set; }
    }
}
