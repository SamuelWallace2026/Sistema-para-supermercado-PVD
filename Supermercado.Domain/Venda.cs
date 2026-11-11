namespace Supermercado.Domain.Entities;

public class Venda
{
    public int Id { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.Now;
    public decimal TotalVenda { get; set; }
    public decimal ValorPago { get; set; }
    public decimal Troco { get; set; }
    public string FormaPagamento { get; set; } = "Dinheiro";
    public List<ItemVenda> Itens { get; set; } = new();
}