using Esforco.Dominio;

namespace Esforco.Aplicacao;

public interface ILeitorDeJanelaDaFrente
{
    AmostraDeJanela? Ler();

    AmostraDeJanela? LerReuniaoMinimizada();

    /// <summary>
    /// As janelas que existem agora, na frente ou não. É só consulta: nenhuma janela é mexida.
    /// </summary>
    IReadOnlyList<JanelaAberta> LerAbertas();
}
