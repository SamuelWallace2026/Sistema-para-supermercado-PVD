namespace Supermercado.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty; // Pode ser texto simples para facilitar, ou hash se preferir
    public string Perfil { get; set; } = "Operador"; // "Administrador" ou "Operador"
}