using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataToolkit.SampleApi.Models;

[Table("Cliente")]
public class Cliente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id", Order = 0)]
    public int Id { get; set; }

    [Column("Nombre", Order = 2)]
    public string Nombre { get; set; }

    [Column("Apellido", Order = 3)]
    public string Apellido { get; set; }

    [Column("Email", Order = 4)]
    public string Email { get; set; }
}
