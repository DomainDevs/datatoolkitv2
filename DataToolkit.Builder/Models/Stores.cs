namespace DataToolkit.Builder.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace DataToolkit.SampleApi.Models
    {
        [Table("stores", Schema = "dbo")]
        public class Stores
        {
            // store_id
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            [Column("store_id")]
            public int StoreId { get; set; }

            // store_name
            [Required]
            [MaxLength(255)]
            [Column("store_name")]
            public string? StoreName { get; set; }

            // phone
            [MaxLength(25)]
            [Column("phone")]
            public string? Phone { get; set; }

            // email
            [MaxLength(255)]
            [Column("email")]
            public string Email { get; set; }

            // street
            [MaxLength(255)]
            [Column("street")]
            public string Street { get; set; }

            // city
            [MaxLength(255)]
            [Column("city")]
            public string City { get; set; }

            // state
            [MaxLength(10)]
            [Column("state")]
            public string State { get; set; }

            // zip_code
            [MaxLength(5)]
            [Column("zip_code")]
            public string ZipCode { get; set; }

            // id_dpto
            [Column("id_dpto")]
            public int? IdDpto { get; set; }

            // precio
            [Column("precio", TypeName = "decimal(18,2)")]
            public decimal? Precio { get; set; }

        }
    }
}
