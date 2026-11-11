using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Supermercado.Data;
using Supermercado.Domain.Entities;

namespace Supermercado.PDV;

public partial class MainWindow : Window
{
    public ObservableCollection<ItemVenda> ListaItens { get; set; } = new();
    private decimal subtotalGeral = 0;
    private string cpfClienteAtual = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        dgItensVenda.ItemsSource = ListaItens;

        TentarInicializarBanco();
        txtCodigoBarras.Focus();

        this.PreviewKeyDown += Window_PreviewKeyDown;
    }

    private void TentarInicializarBanco()
    {
        try
        {
            DbInitializer.Seed();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(
                $"Aviso: Não foi possível conectar ao MySQL local.\n\nDetalhes: {ex.Message}", 
                "Aviso de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TxtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtCodigoBarras.Text))
        {
            string textoDigitado = txtCodigoBarras.Text.Trim();

            if (textoDigitado.Contains("*"))
            {
                var partes = textoDigitado.Split('*');
                if (partes.Length == 2 && decimal.TryParse(partes[0], out decimal qtdDesejada))
                {
                    string codigoBarraReal = partes[1];
                    BuscarEAdicionarProduto(codigoBarraReal, qtdDesejada);
                }
                else
                {
                    MessageBox.Show("Formato de quantidade inválido. Use ex: 3*CODIGO", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                BuscarEAdicionarProduto(textoDigitado, 1);
            }

            txtCodigoBarras.Clear();
            e.Handled = true;
        }
    }

    private void BuscarEAdicionarProduto(string codigo, decimal quantidadeInformada)
    {
        try
        {
            using var db = new AppDbContext();
            var produto = db.Produtos.FirstOrDefault(p => p.CodigoBarras == codigo);

            if (produto == null)
            {
                MessageBox.Show("Produto não encontrado!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = new ItemVenda
            {
                ItemNumero = ListaItens.Count + 1,
                ProdutoId = produto.Id,
                Descricao = produto.Descricao,
                Quantidade = quantidadeInformada,
                ValorUnitario = produto.PrecoVenda
            };

            ListaItens.Add(item);

            lblValorUnitario.Text = item.ValorUnitario.ToString("C");
            lblTotalItem.Text = item.Total.ToString("C");

            RecalcularSubtotal();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Erro de Conexão: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RecalcularSubtotal()
    {
        subtotalGeral = ListaItens.Sum(i => i.Total);
        lblSubtotal.Text = subtotalGeral.ToString("N2");
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
        {
            AbrirCpfCupom();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.F2:
                AbrirPesquisaProduto();
                e.Handled = true;
                break;

            case Key.F3:
                AbrirGestaoProdutosADM();
                e.Handled = true;
                break;

            case Key.F4:
                AbrirPainelGerente();
                e.Handled = true;
                break;

            case Key.F5:
                NovaVenda();
                e.Handled = true;
                break;

            case Key.F6:
                PagarItemSelecionado();
                e.Handled = true;
                break;

            case Key.F9:
                AlterarQuantidadeItem();
                e.Handled = true;
                break;

            case Key.F12:
                FinalizarVenda();
                e.Handled = true;
                break;

            case Key.F11:
                ExcluirItemSelecionado();
                e.Handled = true;
                break;

            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    private bool ValidarAcessoAdministrador()
    {
        // Se já for Administrador logado, libera direto
        if (JanelaLogin.UsuarioLogado?.Perfil == "Administrador")
        {
            return true;
        }

        // Se for operador, abre input box para redigir a senha de administrador
        string senhaAdmin = Microsoft.VisualBasic.Interaction.InputBox("Acesso restrito a Gerentes/Administradores.\nDigite a senha do Administrador:", "Autorização Necessária", "");
        
        if (string.IsNullOrWhiteSpace(senhaAdmin))
        {
            return false;
        }

        using var db = new AppDbContext();
        var adminValido = db.Usuarios.Any(u => u.Perfil == "Administrador" && u.Senha == senhaAdmin);

        if (!adminValido)
        {
            MessageBox.Show("Senha de administrador inválida!", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void PagarItemSelecionado()
    {
        if (dgItensVenda.SelectedItem is not ItemVenda itemSelecionado)
        {
            MessageBox.Show("Selecione na lista o item que deseja pagar separadamente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtCodigoBarras.Focus();
            return;
        }

        decimal valorItem = itemSelecionado.Total;

        var janelaPagamento = new JanelaPagamento(valorItem, new List<ItemVenda> { itemSelecionado });
        janelaPagamento.Owner = this;

        if (janelaPagamento.ShowDialog() == true)
        {
            try
            {
                using var db = new AppDbContext();

                var novaVendaParcial = new Venda
                {
                    DataVenda = DateTime.Now,
                    TotalVenda = valorItem,
                    ValorPago = janelaPagamento.ValorPago,
                    Troco = janelaPagamento.Troco,
                    FormaPagamento = janelaPagamento.FormaPagamentoSelecionada,
                    Itens = new List<ItemVenda>()
                };

                novaVendaParcial.Itens.Add(new ItemVenda
                {
                    ProdutoId = itemSelecionado.ProdutoId,
                    Quantidade = itemSelecionado.Quantidade,
                    ValorUnitario = itemSelecionado.ValorUnitario
                });

                var produtoBanco = db.Produtos.Find(itemSelecionado.ProdutoId);
                if (produtoBanco != null)
                {
                    produtoBanco.EstoqueAtual -= itemSelecionado.Quantidade;
                }

                db.Vendas.Add(novaVendaParcial);
                db.SaveChanges();

                ListaItens.Remove(itemSelecionado);

                int numero = 1;
                foreach (var item in ListaItens)
                {
                    item.ItemNumero = numero++;
                }
                dgItensVenda.Items.Refresh();

                RecalcularSubtotal();

                string detalhes = $"Item '{itemSelecionado.Descricao}' pago com sucesso!\n\n" +
                                  $"Valor: {valorItem:C2}\n" +
                                  $"Forma: {novaVendaParcial.FormaPagamento}\n" +
                                  $"Troco: {novaVendaParcial.Troco:C2}\n\n" +
                                  $"A venda principal continua em aberto.";

                var janelaSucesso = new JanelaSucesso(detalhes);
                janelaSucesso.Owner = this;
                janelaSucesso.ShowDialog();
            }
            catch (Exception ex)
            {
                string erroDetalhado = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Erro ao salvar venda parcial no banco:\n\n{erroDetalhado}", "Erro de Persistência", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        txtCodigoBarras.Focus();
    }

    private void AbrirGestaoProdutosADM()
    {
        if (!ValidarAcessoAdministrador())
        {
            txtCodigoBarras.Focus();
            return;
        }

        try
        {
            var backoffice = new JanelaProdutosADM
            {
                Owner = this
            };
            backoffice.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir módulo administrativo:\n\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            txtCodigoBarras.Focus();
        }
    }

    private void NovaVenda()
    {
        if (ListaItens.Any())
        {
            var resultado = MessageBox.Show("Deseja cancelar a venda atual?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resultado == MessageBoxResult.No) return;
        }

        LimparTelaCaixa();
    }

    private void FinalizarVenda()
    {
        if (!ListaItens.Any())
        {
            MessageBox.Show("Não há itens no cupom para finalizar!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var janelaPagamento = new JanelaPagamento(subtotalGeral, ListaItens.ToList())
        {
            Owner = this
        };

        if (janelaPagamento.ShowDialog() == true)
        {
            try
            {
                using var db = new AppDbContext();

                var novaVenda = new Venda
                {
                    DataVenda = DateTime.Now,
                    TotalVenda = subtotalGeral,
                    ValorPago = janelaPagamento.ValorPago,
                    Troco = janelaPagamento.Troco,
                    FormaPagamento = janelaPagamento.FormaPagamentoSelecionada,
                    Itens = new List<ItemVenda>()
                };

                foreach (var item in ListaItens)
                {
                    novaVenda.Itens.Add(new ItemVenda
                    {
                        ProdutoId = item.ProdutoId,
                        Descricao = item.Descricao,
                        Quantidade = item.Quantidade,
                        ValorUnitario = item.ValorUnitario
                    });

                    var produtoBanco = db.Produtos.Find(item.ProdutoId);
                    if (produtoBanco != null)
                    {
                        produtoBanco.EstoqueAtual -= item.Quantidade;
                    }
                }

                db.Vendas.Add(novaVenda);
                db.SaveChanges();

                lblTotalRecebido.Text = janelaPagamento.ValorPago.ToString("C2");
                lblTroco.Text = janelaPagamento.Troco.ToString("C2");

                string detalhes = $"Venda nº {novaVenda.Id} finalizada com sucesso!\n\n" +
                                  $"Total: {novaVenda.TotalVenda:C2}\n" +
                                  $"Forma: {novaVenda.FormaPagamento}\n" +
                                  $"Troco: {novaVenda.Troco:C2}";

                var janelaSucesso = new JanelaSucesso(detalhes);
                janelaSucesso.Owner = this;
                janelaSucesso.ShowDialog();

                LimparTelaCaixa();
            }
            catch (Exception ex)
            {
                string erroDetalhado = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Erro crítico ao salvar venda no banco:\n\n{erroDetalhado}", "Erro de Persistência", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
           txtCodigoBarras.Focus();
        }
    }

    private void LimparTelaCaixa()
    {
        ListaItens.Clear();
        subtotalGeral = 0;
        cpfClienteAtual = string.Empty;

        lblSubtotal.Text = "0,00";
        lblValorUnitario.Text = "R$ 0,00";
        lblTotalItem.Text = "R$ 0,00";
        lblTotalRecebido.Text = "R$ 0,00";
        lblTroco.Text = "R$ 0,00";
        lblCpfCliente.Text = "CPF na Nota: Não informado";

        txtCodigoBarras.Clear();
        txtCodigoBarras.Focus();
    }

    private void ExcluirItemSelecionado()
    {
        if (dgItensVenda.SelectedItem is ItemVenda itemSelecionado)
        {
            ListaItens.Remove(itemSelecionado);

            int numero = 1;
            foreach (var item in ListaItens)
            {
                item.ItemNumero = numero++;
            }
            dgItensVenda.Items.Refresh();

            RecalcularSubtotal();
            MessageBox.Show("Item excluído com sucesso.", "PDV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Selecione na tabela o item que deseja excluir.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        txtCodigoBarras.Focus();
    }

    private void AlterarQuantidadeItem()
    {
        if (dgItensVenda.SelectedItem is ItemVenda itemSelecionado)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Digite a nova quantidade:", "Alterar Quantidade", itemSelecionado.Quantidade.ToString());
            if (decimal.TryParse(input, out decimal novaQtd) && novaQtd > 0)
            {
                itemSelecionado.Quantidade = novaQtd;
                dgItensVenda.Items.Refresh();
                RecalcularSubtotal();
                
                lblTotalItem.Text = itemSelecionado.Total.ToString("C");
            }
        }
        else
        {
            MessageBox.Show("Selecione um item na tabela para alterar a quantidade.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        txtCodigoBarras.Focus();
    }

    private void AbrirPesquisaProduto()
    {
        try
        {
            var janelaPesquisa = new JanelaPesquisaProduto
            {
                Owner = this
            };

            if (janelaPesquisa.ShowDialog() == true && janelaPesquisa.ProdutoSelecionado != null)
            {
                var prod = janelaPesquisa.ProdutoSelecionado;
                BuscarEAdicionarProduto(prod.CodigoBarras, 1);
            }
        }
        catch (Exception ex)
        {
            string mensagemErro = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            MessageBox.Show($"Erro ao abrir janela de pesquisa:\n\n{mensagemErro}", "Falha no F2", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            txtCodigoBarras.Focus();
        }
    }

    private void AbrirCpfCupom()
    {
        string cpf = Microsoft.VisualBasic.Interaction.InputBox("Digite o CPF para o cupom fiscal:", "CPF na Nota", cpfClienteAtual);
        if (cpf != null)
        {
            cpfClienteAtual = cpf.Trim();
            if (string.IsNullOrWhiteSpace(cpfClienteAtual))
            {
                lblCpfCliente.Text = "CPF na Nota: Não informado";
                MessageBox.Show("CPF removido da nota.", "CPF no Cupom", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                lblCpfCliente.Text = $"CPF: {cpfClienteAtual}";
                MessageBox.Show($"CPF {cpfClienteAtual} vinculado ao cupom com sucesso!", "CPF no Cupom", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        txtCodigoBarras.Focus();
    }

    private void AbrirPainelGerente()
    {
        // Exige senha de Administrador caso o operador logado seja comum
        if (!ValidarAcessoAdministrador())
        {
            txtCodigoBarras.Focus();
            return;
        }

        try
        {
            var janelaGerente = new JanelaGerente
            {
                Owner = this
            };
            janelaGerente.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir o Painel do Gerente:\n\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            txtCodigoBarras.Focus();
        }
    }
}