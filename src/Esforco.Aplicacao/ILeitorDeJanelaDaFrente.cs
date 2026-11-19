using Esforco.Dominio;

namespace Esforco.Aplicacao;

public interface ILeitorDeJanelaDaFrente
{
    AmostraDeJanela? Ler();

    AmostraDeJanela? LerReuniaoMinimizada();
}
