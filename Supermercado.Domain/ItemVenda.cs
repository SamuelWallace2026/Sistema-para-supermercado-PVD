namespace Supermercado.Domain.Entities;

public class ItemVenda
{
    public int Id { get; set; }
    public int ItemNumero { get; set; }
    public int ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Total => Quantidade * ValorUnitario;
}