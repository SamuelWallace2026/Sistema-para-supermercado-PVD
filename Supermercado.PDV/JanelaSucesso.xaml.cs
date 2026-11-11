using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Supermercado.PDV;

public partial class JanelaSucesso : Window
{
    private string textoParaImprimir = string.Empty;

    public JanelaSucesso(string mensagemDetalhes, string titulo = "VENDA CONCLUÍDA COM SUCESSO!")
    {
        InitializeComponent();

        textoParaImprimir = $"{titulo}\n\n{mensagemDetalhes}";

        if (FindName("lblTitulo") is System.Windows.Controls.TextBlock lblT)
        {
            lblT.Text = titulo;
        }

        if (lblMensagemDetalhes != null)
        {
            lblMensagemDetalhes.Text = mensagemDetalhes;
        }

        Loaded += (s, e) =>
        {
            btnOk?.Focus();
        };

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.Escape)
            {
                ExecutarImpressaoEFechar();
                e.Handled = true;
            }
        };
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        ExecutarImpressaoEFechar();
    }

    private void ExecutarImpressaoEFechar()
    {
        try
        {
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

            pd.DocumentName = "Comprovante_Venda";
            pd.PrintPage += (s, e) =>
            {
                if (e?.Graphics != null)
                {
                    // Usa fonte monoespaçada para garantir o alinhamento perfeito das colunas do cupom
                    var font = new Font("Consolas", 9);
                    e.Graphics.DrawString(textoParaImprimir, font, Brushes.Black, 10, 10);
                }
            };
            pd.Print();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao enviar comprovante para a impressora:\n{ex.Message}", "Aviso de Impressão", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        DialogResult = true;
        Close();
    }
}