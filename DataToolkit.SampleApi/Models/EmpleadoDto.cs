using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataToolkit.SampleApi.Models
{
    public class EmpleadoDto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        public string Nombre { get; set; }

        [NotMapped]
        [JsonIgnore]
        [ValidateNever]
        public int DepartamentoId { get; set; }

        public Departamento Departamento { get; set; }
    }
}
