using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using Microsoft.EntityFrameworkCore;
using Supermercado.Data;
using Supermercado.Domain.Entities;

namespace Supermercado.PDV;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Configura a cultura pt-BR para exibição de moeda (R$) e datas
        var culture = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag))
        );

        // 2. Garante a conexão, criação do banco de dados e tabelas MySQL/MariaDB
        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();

            // Garante que a tabela de usuários exista
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Usuarios (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Nome VARCHAR(100) NOT NULL,
                    Login VARCHAR(50) NOT NULL UNIQUE,
                    Senha VARCHAR(100) NOT NULL,
                    Perfil VARCHAR(30) DEFAULT 'Operador'
                );
            ");

            // Garante que exista pelo menos um Administrador padrão se a tabela estiver vazia
            if (!db.Usuarios.Any())
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
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível conectar ou inicializar o banco de dados MySQL.\n\nDetalhes: {ex.Message}",
                "Erro de Conexão com Banco",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown();
            return;
        }

        // 3. Cria a Janela Principal em segundo plano (oculta)
        var mainWindow = new MainWindow();
        this.MainWindow = mainWindow;

        // 4. Abre a Tela de Login como modal por cima
        var loginWindow = new JanelaLogin();
        bool? resultado = loginWindow.ShowDialog();

        // 5. Verifica se o login foi efetuado com sucesso
        if (resultado == true && JanelaLogin.UsuarioLogado != null)
        {
            // Mostra a janela principal do PDV
            mainWindow.Show();
        }
        else
        {
            // Se fechou o login sem autenticar, encerra a aplicação com segurança
            mainWindow.Close();
            Shutdown();
        }
    }
}