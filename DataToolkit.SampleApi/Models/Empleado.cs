using DataToolkit.SampleApi.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

[Table("Empleado")]
public class Empleado
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    [JsonIgnore]
    public int Id { get; set; }

    public string Nombre { get; set; }

    //[ValidateNever]
    public int DepartamentoId { get; set; }

    //[JsonIgnore] //Lo oculta totalmente entradas o salidas
    [NotMapped] //No persiste a la base de datos
    public Departamento Departamento { get; set; }
}