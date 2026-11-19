using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Testes.Apoio;

internal sealed class RelogioManual : IRelogio
{
    public DateTimeOffset Instante { get; set; }

    public DateTimeOffset Agora() => Instante;
}

internal sealed class LeitorFalso : ILeitorDeJanelaDaFrente
{
    public int Leituras { get; private set; }

    public AmostraDeJanela? Proxima { get; set; }

    public AmostraDeJanela? ReuniaoMinimizada { get; set; }

    public Exception? Falha { get; set; }

    public AmostraDeJanela? Ler()
    {
        Leituras++;
        if (Falha is not null)
            throw Falha;
        return Proxima;
    }

    public AmostraDeJanela? LerReuniaoMinimizada() => ReuniaoMinimizada;

    public List<JanelaAberta> Abertas { get; } = [];

    public int LeiturasDeAbertas { get; private set; }

    public Exception? FalhaNasAbertas { get; set; }

    public IReadOnlyList<JanelaAberta> LerAbertas()
    {
        LeiturasDeAbertas++;
        if (FalhaNasAbertas is not null)
            throw FalhaNasAbertas;
        return Abertas.ToList();
    }
}

internal sealed class RegrasFixas : IFonteDeRegrasDeEscritorio
{
    public IReadOnlyList<RegraDeEscritorio> Regras { get; set; } = [];

    public IReadOnlyList<RegraDeEscritorio> Carregar() => Regras;
}

internal static class Cenario
{
    public static (ServicoDeEsforco Servico, LeitorFalso Leitor, RepositorioEmMemoria Repositorio, RelogioManual Relogio, RegrasFixas Regras) Criar()
    {
        var leitor = new LeitorFalso();
        var repositorio = new RepositorioEmMemoria();
        var relogio = new RelogioManual
        {
            Instante = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(-3)),
        };
        var regras = new RegrasFixas();
        var servico = new ServicoDeEsforco(leitor, repositorio, regras, relogio);
        return (servico, leitor, repositorio, relogio, regras);
    }
}
