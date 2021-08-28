using System.ComponentModel.DataAnnotations;

namespace ElasticDbTenants.TenantDb.Models
{
    public class TodoItem
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(128)]
        public string Text { get; set; }
        public bool IsDone { get; set; }
    }
}
