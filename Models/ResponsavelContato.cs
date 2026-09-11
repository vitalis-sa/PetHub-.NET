using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

[Table("TB_RESPONSAVEL_CONTATO")]
public class ResponsavelContato
{
    [Key][Column("ID")] public long Id { get; set; }

    [Column("RESPONSAVEL_ID")] public long ResponsavelId { get; set; }
    [ForeignKey("ResponsavelId")][JsonIgnore]
    public Responsavel? Responsavel { get; set; }

    [Column("TIPO")][Required] public string Tipo { get; set; } = null!;  // CELULAR | TELEFONE

    [Column("TELEFONE")][Required] public string Telefone { get; set; } = null!;  // era Valor

    [Column("PRINCIPAL")] public bool Principal { get; set; }
}