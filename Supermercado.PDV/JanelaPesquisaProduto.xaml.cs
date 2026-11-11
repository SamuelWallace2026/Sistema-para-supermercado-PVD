using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Supermercado.Data;
using Supermercado.Domain.Entities;

namespace Supermercado.PDV;

public partial class JanelaPesquisaProduto : Window
{
    public Produto? ProdutoSelecionado { get; private set; }
    private List<Produto> todosProdutos = new();

    public JanelaPesquisaProduto()
    {
        InitializeComponent();
        Loaded += JanelaPesquisaProduto_Loaded;
    }

    private void JanelaPesquisaProduto_Loaded(object sender, RoutedEventArgs e)
    {
        CarregarProdutos();
        txtPesquisa.Focus();
    }

    private void CarregarProdutos()
    {
        try
        {
            using var db = new AppDbContext();
            todosProdutos = db.Produtos.AsNoTracking().ToList();
            dgProdutos.ItemsSource = todosProdutos;

            if (todosProdutos.Count > 0)
            {
                dgProdutos.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao consultar produtos no banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPesquisa_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (todosProdutos == null) return;

        string termo = txtPesquisa.Text.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(termo))
        {
            dgProdutos.ItemsSource = todosProdutos;
        }
        else
        {
            var filtrados = todosProdutos.Where(p => 
                (!string.IsNullOrEmpty(p.Descricao) && p.Descricao.ToLower().Contains(termo)) || 
                (!string.IsNullOrEmpty(p.CodigoBarras) && p.CodigoBarras.Contains(termo)) ||
                p.Id.ToString().Contains(termo)
            ).ToList();

            dgProdutos.ItemsSource = filtrados;
        }

        if (dgProdutos.Items.Count > 0)
        {
            dgProdutos.SelectedIndex = 0;
        }
    }

    private void ConfirmarSelecao()
    {
        if (dgProdutos.SelectedItem is Produto produto)
        {
            ProdutoSelecionado = produto;
            DialogResult = true;
            Close();
        }
    }

    // Captura as teclas ANTES de qualquer controle interno (DataGrid/TextBox) mudar o foco
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true; // Bloqueia a descida de linha no DataGrid/TextBox
            ConfirmarSelecao();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            DialogResult = false;
            Close();
        }
        else if (e.Key == Key.Down)
        {
            e.Handled = true;
            if (dgProdutos.Items.Count > 0 && dgProdutos.SelectedIndex < dgProdutos.Items.Count - 1)
            {
                dgProdutos.SelectedIndex++;
                dgProdutos.ScrollIntoView(dgProdutos.SelectedItem);
            }
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = true;
            if (dgProdutos.Items.Count > 0 && dgProdutos.SelectedIndex > 0)
            {
                dgProdutos.SelectedIndex--;
                dgProdutos.ScrollIntoView(dgProdutos.SelectedItem);
            }
        }
    }

    private void DgProdutos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ConfirmarSelecao();
    }
}