using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

[Table("TB_RESPONSAVEL_ENDERECO")]
public class ResponsavelEndereco
{
    [Key][Column("ID")] public long Id { get; set; }

    [Column("RESPONSAVEL_ID")] public long ResponsavelId { get; set; }
    [ForeignKey("ResponsavelId")][JsonIgnore]
    public Responsavel? Responsavel { get; set; }

    [Column("LOGRADOURO")][Required] public string Logradouro { get; set; } = null!;
    [Column("NUMERO")][Required]    public string Numero { get; set; } = null!;
    [Column("COMPLEMENTO")]         public string? Complemento { get; set; }
    [Column("BAIRRO")][Required]    public string Bairro { get; set; } = null!;
    [Column("CIDADE")][Required]    public string Cidade { get; set; } = null!;

    [Column("ESTADO")][Required, StringLength(2)]
    public string Estado { get; set; } = null!;

    [Column("CEP")][Required, StringLength(8)]
    public string Cep { get; set; } = null!;

    [Column("PRINCIPAL")] public bool Principal { get; set; }
}