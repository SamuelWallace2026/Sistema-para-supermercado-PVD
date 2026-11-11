using Supermercado.Data;
using Supermercado.Domain.Entities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Supermercado.PDV;

public partial class JanelaLogin : Window
{
    public static Usuario? UsuarioLogado { get; private set; }

    public JanelaLogin()
    {
        InitializeComponent();
        txtLogin.Focus();

        // Garante que o admin padrão existe assim que a janela de login abre
        GarantirAdminPadrao();

        // Permite logar apertando Enter na senha
        txtSenha.KeyDown += (s, e) => { if (e.Key == Key.Enter) RealizarLogin(); };
        txtLogin.KeyDown += (s, e) => { if (e.Key == Key.Enter) txtSenha.Focus(); };
    }

    private void GarantirAdminPadrao()
    {
        try
        {
            using var db = new AppDbContext();
            var admin = db.Usuarios.FirstOrDefault(u => u.Login == "admin");
            if (admin == null)
            {
                db.Usuarios.Add(new Usuario 
                { 
                    Nome = "Administrador", 
                    Login = "admin", 
                    Senha = "123", 
                    Perfil = "Administrador" 
                });
                db.SaveChanges();
            }
        }
        catch (Exception)
        {
            // Ignora caso a tabela ainda esteja a ser criada
        }
    }

    private void BtnEntrar_Click(object sender, RoutedEventArgs e)
    {
        RealizarLogin();
    }

    private void RealizarLogin()
    {
        string login = txtLogin.Text.Trim();
        string senha = txtSenha.Password;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            MessageBox.Show("Informe o usuário e a senha!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            
            var usuario = db.Usuarios.FirstOrDefault(u => u.Login == login && u.Senha == senha);

            if (usuario != null)
            {
                UsuarioLogado = usuario;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Usuário ou senha incorretos!", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSenha.Clear();
                txtSenha.Focus();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao conectar com o banco de dados:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}