namespace Supermercado.Domain.Entities;

public class Produto
{
    public int Id { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoVenda { get; set; }
    public decimal PrecoCusto { get; set; }
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; } = 5;
    public string UnidadeMedida { get; set; } = "UN"; // UN, KG, LT

    // Relacionamento com Categoria
    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
}