using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Supermercado.PDV;

public class ItemPagamento
{
    public string Forma { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public partial class JanelaPagamento : Window
{
    private decimal totalVenda;
    public ObservableCollection<ItemPagamento> ListaPagamentos { get; set; } = new();

    public decimal ValorPago => ListaPagamentos.Sum(p => p.Valor);
    public decimal Troco => ValorPago > totalVenda ? ValorPago - totalVenda : 0;
    public string FormaPagamentoSelecionada => string.Join(" + ", ListaPagamentos.Select(p => $"{p.Forma} ({p.Valor:C2})"));

    public JanelaPagamento(decimal total, List<Domain.Entities.ItemVenda> itens)
    {
        InitializeComponent();
        totalVenda = total;
        dgPagamentos.ItemsSource = ListaPagamentos;

        lblTotalPagar.Text = totalVenda.ToString("C2");
        AtualizarSaldos();

        Loaded += (s, e) =>
        {
            txtValorAdicionar.Focus();
            txtValorAdicionar.SelectAll();
        };

        PreviewKeyDown += JanelaPagamento_PreviewKeyDown;
    }

    private void AtualizarSaldos()
    {
        decimal recebido = ValorPago;
        decimal restante = totalVenda - recebido;

        if (restante > 0)
        {
            lblFaltaPagar.Text = restante.ToString("C2");
            brdRestante.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#842029")!;
            txtValorAdicionar.Text = restante.ToString("F2");
        }
        else
        {
            lblFaltaPagar.Text = "R$ 0,00";
            brdRestante.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#0F5132")!;
            txtValorAdicionar.Text = "0,00";
        }

        lblTroco.Text = Troco.ToString("C2");
        txtValorAdicionar.SelectAll();
    }

    private void AdicionarPagamento()
    {
        if (!decimal.TryParse(txtValorAdicionar.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) || valor <= 0)
        {
            MessageBox.Show("Informe um valor válido para adicionar!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtValorAdicionar.Focus();
            return;
        }

        string forma = (cbFormaPagamento.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Dinheiro";

        ListaPagamentos.Add(new ItemPagamento { Forma = forma, Valor = valor });

        AtualizarSaldos();
        txtValorAdicionar.Focus();
    }

    private void BtnAdicionarPagamento_Click(object sender, RoutedEventArgs e)
    {
        AdicionarPagamento();
    }

    private void TxtValorAdicionar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AdicionarPagamento();
            e.Handled = true;
        }
    }

    private void BtnRemoverPagamento_Click(object sender, RoutedEventArgs e)
    {
        RemoverItemSelecionado();
    }

    private void RemoverItemSelecionado()
    {
        if (dgPagamentos.SelectedItem is ItemPagamento item)
        {
            ListaPagamentos.Remove(item);
            AtualizarSaldos();
        }
        else
        {
            MessageBox.Show("Selecione um pagamento lançado para remover.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnFinalizar_Click(object sender, RoutedEventArgs e)
    {
        ConfirmarFinalizacao();
    }

    private void ConfirmarFinalizacao()
    {
        if (ValorPago < totalVenda)
        {
            MessageBox.Show($"O valor total pago ({ValorPago:C2}) ainda é menor que o total da venda ({totalVenda:C2})!",
                "Pagamento Incompleto", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtValorAdicionar.Focus();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void JanelaPagamento_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.F12)
        {
            ConfirmarFinalizacao();
            e.Handled = true;
        }
        else if (e.Key == Key.F11)
        {
            RemoverItemSelecionado();
            e.Handled = true;
        }
    }
}