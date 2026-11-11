using Microsoft.EntityFrameworkCore;
using Supermercado.Data;
using Supermercado.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Supermercado.PDV;

public partial class JanelaProdutosADM : Window
{
    private List<Produto> todosProdutos = new();
    private Produto? produtoEmEdicao = null;

    public JanelaProdutosADM()
    {
        InitializeComponent();
        Loaded += JanelaProdutosADM_Loaded;

        if (FindName("txtCodigoBarras") is TextBox txtBarra)
        {
            txtBarra.KeyDown += TxtCodigoBarras_KeyDown;
        }
    }

    private void JanelaProdutosADM_Loaded(object sender, RoutedEventArgs e)
    {
        CarregarCategorias();
        CarregarProdutos();
    }

    private void CarregarCategorias()
    {
        try
        {
            using var db = new AppDbContext();
            
            if (!db.Categorias.Any())
            {
                db.Categorias.AddRange(
                    new Categoria { Nome = "Mercearia" },
                    new Categoria { Nome = "Bebidas" },
                    new Categoria { Nome = "Laticínios" },
                    new Categoria { Nome = "Hortifruti" },
                    new Categoria { Nome = "Limpeza" }
                );
                db.SaveChanges();
            }

            var categorias = db.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToList();
            cmbCategoria.ItemsSource = categorias;
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao carregar categorias: {ex.Message}", "ERRO", this);
        }
    }

    private void CarregarProdutos()
    {
        try
        {
            using var db = new AppDbContext();
            todosProdutos = db.Produtos
                .Include(p => p.Categoria)
                .AsNoTracking()
                .OrderBy(p => p.Descricao)
                .ToList();

            AplicarFiltros();
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao carregar lista de produtos: {ex.Message}", "ERRO", this);
        }
    }

    private void AplicarFiltros()
    {
        var listaFiltrada = todosProdutos.AsEnumerable();

        string termo = txtFiltro.Text.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            listaFiltrada = listaFiltrada.Where(p =>
                (!string.IsNullOrEmpty(p.Descricao) && p.Descricao.ToLower().Contains(termo)) ||
                (!string.IsNullOrEmpty(p.CodigoBarras) && p.CodigoBarras.Contains(termo)) ||
                p.Id.ToString().Contains(termo)
            );
        }

        if (chkEstoqueCritico.IsChecked == true)
        {
            listaFiltrada = listaFiltrada.Where(p => p.EstoqueAtual <= p.EstoqueMinimo);
        }

        var resultado = listaFiltrada.ToList();
        dgProdutos.ItemsSource = resultado;

        int totalAvisos = todosProdutos.Count(p => p.EstoqueAtual <= p.EstoqueMinimo);
        lblTotalProdutos.Text = $"Exibindo {resultado.Count} de {todosProdutos.Count} produto(s) | ⚠️ {totalAvisos} em estoque crítico";
    }

    // --- LEITOR DE CÓDIGO DE BARRAS INTELIGENTE ---
    private void TxtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtCodigoBarras.Text))
        {
            string codigo = txtCodigoBarras.Text.Trim();
            using var db = new AppDbContext();
            var produtoExistente = db.Produtos.FirstOrDefault(p => p.CodigoBarras == codigo);

            if (produtoExistente != null)
            {
                produtoEmEdicao = produtoExistente;
                txtDescricao.Text = produtoExistente.Descricao;
                txtPrecoVenda.Text = produtoExistente.PrecoVenda.ToString("F2");
                txtEstoqueAtual.Text = produtoExistente.EstoqueAtual.ToString("F3");
                txtEstoqueMinimo.Text = produtoExistente.EstoqueMinimo.ToString("F3");
                if (produtoExistente.CategoriaId.HasValue)
                {
                    cmbCategoria.SelectedValue = produtoExistente.CategoriaId.Value;
                }
                MensagemPDV.Mostrar("Produto já cadastrado! Carregado para edição.", "PRODUTO ENCONTRADO", this);
            }
            else
            {
                produtoEmEdicao = null;
                txtDescricao.Clear();
                txtPrecoVenda.Clear();
                txtEstoqueAtual.Text = "1";
                txtEstoqueMinimo.Text = "5";
                MensagemPDV.Mostrar("Código novo lido! Preencha os dados e clique em Salvar Produto.", "NOVO PRODUTO", this);
                txtDescricao.Focus();
            }
            e.Handled = true;
        }
    }

    private void LimparFormulario()
    {
        produtoEmEdicao = null;
        txtCodigoBarras.Clear();
        txtDescricao.Clear();
        txtPrecoVenda.Clear();
        txtEstoqueAtual.Clear();
        txtEstoqueMinimo.Text = "5";
        cmbCategoria.SelectedIndex = -1;

        dgProdutos.UnselectAll();
        txtCodigoBarras.Focus();
    }

    private void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        LimparFormulario();
    }

    private void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtDescricao.Text))
        {
            MensagemPDV.Mostrar("Informe a descrição do produto!", "AVISO", this);
            txtDescricao.Focus();
            return;
        }

        if (!decimal.TryParse(txtPrecoVenda.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco <= 0)
        {
            MensagemPDV.Mostrar("Informe um preço de venda válido!", "AVISO", this);
            txtPrecoVenda.Focus();
            return;
        }

        if (!decimal.TryParse(txtEstoqueAtual.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal estoqueAtual))
        {
            estoqueAtual = 0;
        }

        if (!decimal.TryParse(txtEstoqueMinimo.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal estoqueMinimo))
        {
            estoqueMinimo = 5;
        }

        try
        {
            using var db = new AppDbContext();

            if (produtoEmEdicao == null)
            {
                var novoProduto = new Produto
                {
                    CodigoBarras = txtCodigoBarras.Text.Trim(),
                    Descricao = txtDescricao.Text.Trim().ToUpper(),
                    PrecoVenda = preco,
                    EstoqueAtual = estoqueAtual,
                    EstoqueMinimo = estoqueMinimo,
                    CategoriaId = cmbCategoria.SelectedValue as int?
                };

                db.Produtos.Add(novoProduto);
            }
            else
            {
                var produtoBanco = db.Produtos.Find(produtoEmEdicao.Id);
                if (produtoBanco != null)
                {
                    produtoBanco.CodigoBarras = txtCodigoBarras.Text.Trim();
                    produtoBanco.Descricao = txtDescricao.Text.Trim().ToUpper();
                    produtoBanco.PrecoVenda = preco;
                    produtoBanco.EstoqueAtual = estoqueAtual;
                    produtoBanco.EstoqueMinimo = estoqueMinimo;
                    produtoBanco.CategoriaId = cmbCategoria.SelectedValue as int?;

                    db.Produtos.Update(produtoBanco);
                }
            }

            db.SaveChanges();
            MensagemPDV.Mostrar("Produto salvo com sucesso no estoque!", "SUCESSO", this);

            LimparFormulario();
            CarregarProdutos();
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao salvar produto: {ex.Message}", "ERRO", this);
        }
    }

    private void BtnExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (produtoEmEdicao == null)
        {
            MensagemPDV.Mostrar("Selecione um produto na tabela para excluir!", "AVISO", this);
            return;
        }

        if (MessageBox.Show($"Deseja realmente excluir o produto '{produtoEmEdicao.Descricao}'?",
            "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                using var db = new AppDbContext();
                var produtoBanco = db.Produtos.Find(produtoEmEdicao.Id);
                if (produtoBanco != null)
                {
                    db.Produtos.Remove(produtoBanco);
                    db.SaveChanges();
                    MensagemPDV.Mostrar("Produto excluído com sucesso!", "SUCESSO", this);
                }

                LimparFormulario();
                CarregarProdutos();
            }
            catch (Exception ex)
            {
                MensagemPDV.Mostrar($"Erro ao excluir produto: {ex.Message}", "ERRO", this);
            }
        }
    }

    private void DgProdutos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgProdutos.SelectedItem is Produto prod)
        {
            produtoEmEdicao = prod;
            txtCodigoBarras.Text = prod.CodigoBarras;
            txtDescricao.Text = prod.Descricao;
            txtPrecoVenda.Text = prod.PrecoVenda.ToString("F2");
            txtEstoqueAtual.Text = prod.EstoqueAtual.ToString("F3");
            txtEstoqueMinimo.Text = prod.EstoqueMinimo.ToString("F3");

            if (prod.CategoriaId.HasValue)
            {
                cmbCategoria.SelectedValue = prod.CategoriaId.Value;
            }
            else
            {
                cmbCategoria.SelectedIndex = -1;
            }
        }
    }

    private void TxtFiltro_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltros();
    }

    private void ChkEstoqueCritico_Changed(object sender, RoutedEventArgs e)
    {
        AplicarFiltros();
    }

    // --- IMPRESSÃO DIRETA DA LISTAGEM DE PRODUTOS ---
    private void BtnImprimirLista_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var relatorio = "=== RELATÓRIO DE PRODUTOS - SUPERMERCADO SUPERFORQUILHA ===\n";
            relatorio += $"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n";
            relatorio += "--------------------------------------------------------\n";
            relatorio += "CÓDIGO      DESCRIÇÃO                        PREÇO     ESTOQUE\n";
            relatorio += "--------------------------------------------------------\n";

            if (dgProdutos.ItemsSource is IEnumerable<Produto> produtosExibidos)
            {
                foreach (var p in produtosExibidos)
                {
                    if (p == null) continue;

                    string cod = (p.CodigoBarras ?? "").PadRight(12);
                    if (cod.Length > 12) cod = cod.Substring(0, 12);

                    string desc = (p.Descricao ?? "").PadRight(32);
                    if (desc.Length > 32) desc = desc.Substring(0, 32);

                    string preco = p.PrecoVenda.ToString("C2").PadLeft(9);
                    string estoque = p.EstoqueAtual.ToString("N3").PadLeft(9);

                    relatorio += $"{cod} {desc} {preco} {estoque}\n";
                }
            }

            relatorio += "--------------------------------------------------------\n";

            PrintDocument pd = new PrintDocument();

            // Lê a impressora térmica salva nas configurações, se existir
            if (File.Exists("impressora_config.txt"))
            {
                string impressoraSalva = File.ReadAllText("impressora_config.txt").Trim();
                if (!string.IsNullOrEmpty(impressoraSalva))
                {
                    pd.PrinterSettings.PrinterName = impressoraSalva;
                }
            }

            pd.DocumentName = "Relatorio_Produtos";
            pd.PrintPage += (s, ev) =>
            {
                if (ev?.Graphics != null)
                {
                    var font = new Font("Consolas", 8.5f);
                    ev.Graphics.DrawString(relatorio, font, Brushes.Black, 10, 10);
                }
            };
            pd.Print();

            MensagemPDV.Mostrar("Relatório de produtos enviado para a impressora com sucesso!", "IMPRESSÃO", this);
        }
        catch (Exception ex)
        {
            MensagemPDV.Mostrar($"Erro ao imprimir relatório: {ex.Message}", "ERRO", this);
        }
    }
}