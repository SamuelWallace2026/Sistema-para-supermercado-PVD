using System.Windows;

namespace Supermercado.PDV;

public static class MensagemPDV
{
    public static void Mostrar(string mensagem, string titulo = "AVISO DO SISTEMA", Window? owner = null)
    {
        var janela = new JanelaSucesso(mensagem, titulo);
        if (owner != null)
        {
            janela.Owner = owner;
        }
        else if (Application.Current != null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
        {
            janela.Owner = Application.Current.MainWindow;
        }

        janela.ShowDialog();
    }
}