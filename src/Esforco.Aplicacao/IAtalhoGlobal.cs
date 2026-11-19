using Esforco.Dominio;

namespace Esforco.Aplicacao;

/// <summary>
/// Atalho de teclado que vale com qualquer programa na frente, enquanto o Esforço estiver aberto.
/// </summary>
public interface IAtalhoGlobal
{
    /// <summary>
    /// Número da mensagem que a janela recebe quando o atalho é apertado.
    /// </summary>
    int Mensagem { get; }

    /// <summary>
    /// Devolve falso quando o Windows recusa, por exemplo porque outro programa já usa a combinação.
    /// </summary>
    bool Registrar(IntPtr janela, int id, AtalhoDeTeclado atalho);

    void Liberar(IntPtr janela, int id);
}
