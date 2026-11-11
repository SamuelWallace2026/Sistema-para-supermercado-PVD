using Microsoft.EntityFrameworkCore;
using Supermercado.Domain.Entities;

namespace Supermercado.Data;

public class AppDbContext : DbContext
{
    public DbSet<Produto> Produtos { get; set; } = null!;
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<ItemVenda> ItensVenda { get; set; } = null!;
    public DbSet<Venda> Vendas { get; set; } = null!;
    public DbSet<Fornecedor> Fornecedores { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = 
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeamento de Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
        });

        // Mapeamento de Produto
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CodigoBarras).IsUnique();
            entity.Property(e => e.CodigoBarras).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(150).IsRequired();
            entity.Property(e => e.PrecoVenda).HasPrecision(10, 2);
            entity.Property(e => e.PrecoCusto).HasPrecision(10, 2);
            entity.Property(e => e.EstoqueAtual).HasPrecision(10, 3);
            entity.Property(e => e.EstoqueMinimo).HasPrecision(10, 3);
            entity.Property(e => e.UnidadeMedida).HasMaxLength(10).HasDefaultValue("UN");

            entity.HasOne(p => p.Categoria)
                  .WithMany()
                  .HasForeignKey(p => p.CategoriaId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Mapeamento de ItemVenda
        modelBuilder.Entity<ItemVenda>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ValorUnitario).HasPrecision(10, 2);
            entity.Property(e => e.Quantidade).HasPrecision(10, 3);
        });

        // Mapeamento de Venda
        modelBuilder.Entity<Venda>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalVenda).HasPrecision(10, 2);
            entity.Property(e => e.ValorPago).HasPrecision(10, 2);
            entity.Property(e => e.Troco).HasPrecision(10, 2);
            entity.Property(e => e.FormaPagamento).HasColumnType("TEXT").IsRequired();
            entity.HasMany(e => e.Itens).WithOne().OnDelete(DeleteBehavior.Cascade);
        });

        // Mapeamento de Fornecedor
        modelBuilder.Entity<Fornecedor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomeFantasia).HasMaxLength(150).IsRequired();
            entity.Property(e => e.RazaoSocial).HasMaxLength(150);
            entity.Property(e => e.Cnpj).HasMaxLength(20);
            entity.Property(e => e.Telefone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
        });

        // Mapeamento de Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Login).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Login).IsUnique();
            entity.Property(e => e.Senha).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Perfil).HasMaxLength(30).HasDefaultValue("Operador");
        });
    }
}
