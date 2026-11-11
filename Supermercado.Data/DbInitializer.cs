using Supermercado.Domain.Entities;
using System.Linq;

namespace Supermercado.Data;

public static class DbInitializer
{
    public static void Seed()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        if (!db.Produtos.Any())
        {
            db.Produtos.AddRange(
                new Produto { CodigoBarras = "7891000000011", Descricao = "ARROZ BRANCO 5KG", PrecoVenda = 26.90m, PrecoCusto = 20.00m, EstoqueAtual = 50 },
                new Produto { CodigoBarras = "7891000000028", Descricao = "FEIJÃO CARIOCA 1KG", PrecoVenda = 8.50m, PrecoCusto = 5.50m, EstoqueAtual = 100 },
                new Produto { CodigoBarras = "7891000000035", Descricao = "ÓLEO DE SOJA 900ML", PrecoVenda = 6.99m, PrecoCusto = 4.80m, EstoqueAtual = 80 },
                new Produto { CodigoBarras = "7891000000042", Descricao = "AÇÚCAR REFINADO 1KG", PrecoVenda = 4.29m, PrecoCusto = 3.10m, EstoqueAtual = 120 },
                new Produto { CodigoBarras = "7891000000059", Descricao = "LEITE INTEGRAL 1L", PrecoVenda = 5.49m, PrecoCusto = 3.90m, EstoqueAtual = 200 }
            );

            db.SaveChanges();
        }
    }
}