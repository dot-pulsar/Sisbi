using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("city")]
    public class City
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("name")] public string Name { get; set; }
    }
}