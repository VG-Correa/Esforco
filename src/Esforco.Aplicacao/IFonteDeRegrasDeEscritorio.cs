using Esforco.Dominio;

namespace Esforco.Aplicacao;

public interface IFonteDeRegrasDeEscritorio
{
    IReadOnlyList<RegraDeEscritorio> Carregar();
}
