using Vitalis.Models;

public class CriarLembreteDto
{
    public long ResponsavelId { get; set; }
    public long PetId { get; set; }
    public TipoLembrete Tipo { get; set; }
    public DateOnly DataAgendada { get; set; }
    public string Mensagem { get; set; } = null!;
    public long? ReferenciaId { get; set; }
    public string? ReferenciaTipo { get; set; }
}