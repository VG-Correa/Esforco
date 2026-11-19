using Esforco.Dominio;

namespace Esforco.Aplicacao;

public interface ILeitorDeJanelaDaFrente
{
    AmostraDeJanela? Ler();

    AmostraDeJanela? LerReuniaoMinimizada();

    /// <summary>
    /// As janelas que existem agora, na frente ou não. Com abas, cada janela de navegador traz
    /// também os nomes das abas dela. É só consulta: nenhuma janela é mexida.
    /// </summary>
    IReadOnlyList<JanelaAberta> LerAbertas(bool comAbas);
}
