using Microsoft.EntityFrameworkCore;
using Supermercado.Data;
using Supermercado.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows;

namespace Supermercado.PDV;

public partial class JanelaGerente : Window
{
    public JanelaGerente()
    {
        InitializeComponent();
        dpFiltroData.SelectedDate = DateTime.Today;
        CarregarImpressoras();
        CarregarDadosGerenciais();
    }

    private void CarregarDadosGerenciais()
    {
        CarregarVendas();
        CarregarEstoque();
        CarregarFornecedores();
        CalcularFechamentoCaixa(DateTime.Today);
    }

    // --- MÉTODO AUXILIAR DE IMPRESSÃO DIRECIONADA ---
    private void ImprimirTextoNaImpressoraConfigurada(string textoParaImprimir, string nomeDocumento = "Comprovante")
    {
        try
        {
            PrintDocument pd = new PrintDocument();

            // Define a impressora salva nas configurações, se existir
            if (File.Exists("impressora_config.txt"))
            {
                string impressoraSalva = File.ReadAllText("impressora_config.txt").Trim();
                if (!string.IsNullOrEmpty(impressoraSalva))
                {
                    pd.PrinterSettings.PrinterName = impressoraSalva;
                }
            }

            pd.DocumentName = nomeDocumento;
            pd.PrintPage += (s, e) =>
            {
                if (e?.Graphics != null)
                {
                    var font = new System.Drawing.Font("Consolas", 9);
                    e.Graphics.DrawString(textoParaImprimir, font, System.Drawing.Brushes.Black, 10, 10);
                }
            };

            pd.Print();
        }
        catch (Exception ex)
        {
            throw new Exception($"Não foi possível enviar para a impressora. Verifique se a impressora está online e configurada.\nDetalhes: {ex.Message}");
        }
    }

    // --- ABA 1: VENDAS E COMPROVANTES ---
    private void CarregarVendas()
    {
        try
        {
            using var db = new AppDbContext();
            var dataFiltro = dpFiltroData.SelectedDate ?? DateTime.Today;
            
            var vendasDoDia = db.Vendas
                .Where(v => v.DataVenda.Date == dataFiltro.Date)
                .OrderByDescending(v => v.DataVenda)
                .ToList();

            dgVendas.ItemsSource = vendasDoDia;
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao carregar vendas: {ex.Message}", "ERRO DE BANCO", this);
        }
    }

    private void DpFiltroData_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        CarregarVendas();
        if (dpFiltroData.SelectedDate.HasValue)
        {
            CalcularFechamentoCaixa(dpFiltroData.SelectedDate.Value);
        }
    }

    private void BtnAtualizarVendas_Click(object sender, RoutedEventArgs e)
    {
        CarregarVendas();
    }

    private void BtnImprimirComprovante_Click(object sender, RoutedEventArgs e)
    {
        if (dgVendas.SelectedItem is Venda vendaSelecionada)
        {
            try
            {
                using var db = new AppDbContext();
                var vendaCompleta = db.Vendas
                    .Include(v => v.Itens)
                    .FirstOrDefault(v => v.Id == vendaSelecionada.Id);

                if (vendaCompleta == null) return;

                string comprovante = $"==============================================\n" +
                                     $"         SUPERMERCADO SuperForquilha          \n" +
                                     $"           COMPROVANTE DE VENDA              \n" +
                                     $"==============================================\n" +
                                     $"Venda Nº: {vendaCompleta.Id:D6}\n" +
                                     $"Data/Hora: {vendaCompleta.DataVenda:dd/MM/yyyy HH:mm:ss}\n" +
                                     $"----------------------------------------------\n" +
                                     $"#|COD|DESCRIÇÃO\n" +
                                     $" QTD UN x VL UN                                VL TOTAL\n" +
                                     $"----------------------------------------------\n";

                int itemSeq = 1;
                foreach (var item in vendaCompleta.Itens)
                {
                    var produtoDb = db.Produtos.FirstOrDefault(p => p.Id == item.ProdutoId);

                    string codigoBarras = produtoDb?.CodigoBarras ?? "SEM COD";
                    string nomeProduto = produtoDb?.Descricao ?? (!string.IsNullOrWhiteSpace(item.Descricao) ? item.Descricao : "PRODUTO DIVERSO");

                    decimal totalItem = item.Quantidade * item.ValorUnitario;

                    comprovante += $"{itemSeq:D3} {codigoBarras} {nomeProduto}\n";

                    string detalheQtd = $"   {item.Quantidade,5:N3} UN x {item.ValorUnitario,8:N2}";
                    string valorTotalStr = $"{totalItem,8:N2}";
                    
                    int espacos = Math.Max(1, 46 - detalheQtd.Length - valorTotalStr.Length);
                    comprovante += $"{detalheQtd}{new string(' ', espacos)}{valorTotalStr}\n";

                    itemSeq++;
                }

                comprovante += $"----------------------------------------------\n" +
                               $"TOTAL R$: {vendaCompleta.TotalVenda,35:N2}\n" +
                               $"FORMA PAGTO: {vendaCompleta.FormaPagamento}\n" +
                               $"TROCO R$: {vendaCompleta.Troco,35:N2}\n" +
                               $"==============================================\n" +
                               $"        OBRIGADO PELA PREFERÊNCIA!            \n" +
                               $"==============================================\n";

                // Abre a janela de visualização em tela primeiro
                var janelaVisualizacao = new JanelaSucesso(comprovante, $"COMPROVANTE DA VENDA Nº {vendaCompleta.Id:D6}");
                janelaVisualizacao.Owner = this;
                janelaVisualizacao.ShowDialog();
            }
            catch (Exception ex)
            {
                MensagemPDV.Mostrar(ex.Message, "ERRO DE IMPRESSÃO", this);
            }
        }
        else
        {
            MensagemPDV.Mostrar("Selecione uma venda na lista para visualizar/imprimir o comprovante.", "AVISO", this);
        }
    }

    // --- ABA 2: FECHAMENTO DE CAIXA ---
    private void CalcularFechamentoCaixa(DateTime data)
    {
        try
        {
            using var db = new AppDbContext();
            var vendas = db.Vendas.Where(v => v.DataVenda.Date == data.Date).ToList();

            int qtdVendas = vendas.Count;
            decimal totalFaturamento = vendas.Sum(v => v.TotalVenda);

            lblResumoQtdVendas.Text = qtdVendas.ToString();
            lblResumoFaturamento.Text = totalFaturamento.ToString("C2");

            string relatorio = $"=== RELATÓRIO DE FECHAMENTO DE CAIXA ===\n" +
                               $"Data Referência: {data:dd/MM/yyyy}\n" +
                               $"Total de Vendas: {qtdVendas}\n" +
                               $"Faturamento Bruto: {totalFaturamento:C2}\n\n" +
                               $"Detalhamento por Transação:\n" +
                               $"----------------------------------------\n";

            foreach (var v in vendas)
            {
                relatorio += $"ID: {v.Id} | Hora: {v.DataVenda:HH:mm} | Valor: {v.TotalVenda:C2} | Forma: {v.FormaPagamento}\n";
            }

            txtRelatorioCaixa.Text = relatorio;
        }
        catch (Exception ex)
        {
            txtRelatorioCaixa.Text = $"Erro ao gerar relatório: {ex.Message}";
        }
    }

    private void BtnGerarFechamento_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string textoRelatorio = txtRelatorioCaixa.Text;
            if (string.IsNullOrWhiteSpace(textoRelatorio))
            {
                MensagemPDV.Mostrar("Não há dados de fechamento para imprimir.", "AVISO", this);
                return;
            }

            // Exibe a visualização em tela do relatório de fechamento antes de enviar para a impressora
            var janelaVisualizacao = new JanelaSucesso(textoRelatorio, "FECHAMENTO DE CAIXA");
            janelaVisualizacao.Owner = this;
            janelaVisualizacao.ShowDialog();
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar(ex.Message, "ERRO DE IMPRESSÃO", this);
        }
    }

    // --- ABA 3: ESTOQUE ---
    private void CarregarEstoque()
    {
        try
        {
            using var db = new AppDbContext();
            dgEstoque.ItemsSource = db.Produtos.OrderBy(p => p.Descricao).ToList();
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao carregar estoque: {ex.Message}", "ERRO", this);
        }
    }

    // --- ABA 4: FORNECEDORES ---
    private void CarregarFornecedores()
    {
        try
        {
            using var db = new AppDbContext();
            dgFornecedores.ItemsSource = db.Fornecedores.ToList();
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao carregar fornecedores: {ex.Message}", "ERRO", this);
        }
    }

    private void BtnNovoFornecedor_Click(object sender, RoutedEventArgs e)
    {
        string nome = Microsoft.VisualBasic.Interaction.InputBox("Digite o Nome Fantasia do Fornecedor:", "Novo Fornecedor", "");
        if (!string.IsNullOrWhiteSpace(nome))
        {
            try
            {
                using var db = new AppDbContext();
                db.Fornecedores.Add(new Fornecedor { NomeFantasia = nome, RazaoSocial = nome });
                db.SaveChanges();
                CarregarFornecedores();
                MensagemPDV.Mostrar("Fornecedor cadastrado com sucesso!", "SUCESSO", this);
            }
            catch (Exception ex)
            {
                MensagemPDV.Mostrar($"Erro ao salvar fornecedor: {ex.Message}", "ERRO", this);
            }
        }
    }

    private void BtnExcluirFornecedor_Click(object sender, RoutedEventArgs e)
    {
        if (dgFornecedores.SelectedItem is Fornecedor fornecedorSelecionado)
        {
            if (MessageBox.Show($"Deseja excluir o fornecedor '{fornecedorSelecionado.NomeFantasia}'?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new AppDbContext();
                    db.Fornecedores.Remove(fornecedorSelecionado);
                    db.SaveChanges();
                    CarregarFornecedores();
                    MensagemPDV.Mostrar("Fornecedor removido com sucesso.", "REMOVIDO", this);
                }
                catch (Exception ex)
                {
                    MensagemPDV.Mostrar($"Erro ao excluir: {ex.Message}", "ERRO", this);
                }
            }
        }
        else
        {
            MensagemPDV.Mostrar("Selecione um fornecedor na lista.", "AVISO", this);
        }
    }

    // --- ABA 5: CONFIGURAÇÕES DE IMPRESSORA ---
    private void CarregarImpressoras()
    {
        try
        {
            cbImpressoras.Items.Clear();
            foreach (string impressora in PrinterSettings.InstalledPrinters)
            {
                cbImpressoras.Items.Add(impressora);
            }

            if (File.Exists("impressora_config.txt"))
            {
                string salva = File.ReadAllText("impressora_config.txt").Trim();
                if (cbImpressoras.Items.Contains(salva))
                {
                    cbImpressoras.SelectedItem = salva;
                }
            }
            else if (cbImpressoras.Items.Count > 0)
            {
                cbImpressoras.SelectedIndex = 0;
            }
        }
        catch 
        {
            // Ignora se houver restrição com impressoras locais do Windows
        }
    }

    private void BtnSalvarImpressora_Click(object sender, RoutedEventArgs e)
    {
        string impressoraEscolhida = cbImpressoras.SelectedItem?.ToString() ?? string.Empty;
        try
        {
            File.WriteAllText("impressora_config.txt", impressoraEscolhida);
            MensagemPDV.Mostrar($"Impressora '{impressoraEscolhida}' definida como padrão para emissão de comprovantes!", "CONFIGURAÇÃO SALVA", this);
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao salvar configuração: {ex.Message}", "ERRO", this);
        }
    }
}