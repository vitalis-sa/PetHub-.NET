using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Responsavel>         Responsavels   { get; set; }
    public DbSet<ResponsavelEndereco> Enderecos { get; set; }
    public DbSet<ResponsavelContato>  Contatos  { get; set; }
    public DbSet<Lembrete>      Lembretes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lembrete>()
            .HasOne(l => l.Responsavel)
            .WithMany(t => t.Lembretes)
            .HasForeignKey(l => l.ResponsavelId);

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.CreatedAt)
            .HasDefaultValueSql("SYSDATE");

        modelBuilder.Entity<Responsavel>()
            .Property(t => t.CreatedAt)
            .HasDefaultValueSql("SYSDATE");

        modelBuilder.Entity<Responsavel>()
            .Property(t => t.Ativo)
            .HasColumnType("NUMBER(1)");

        modelBuilder.Entity<ResponsavelEndereco>()
            .Property(e => e.Principal)
            .HasColumnType("NUMBER(1)");

        modelBuilder.Entity<ResponsavelContato>()
            .Property(c => c.Principal)
            .HasColumnType("NUMBER(1)");
    }
}