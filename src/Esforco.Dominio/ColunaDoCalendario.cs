namespace Esforco.Dominio;

/// <summary>
/// Uma faixa da coluna do dia, com o horário em segundos do dia.
/// </summary>
public readonly record struct FaixaNaColuna(int Inicio, int Fim, bool Manual);

/// <summary>
/// Onde a faixa fica na largura da coluna: a distância da borda esquerda e a largura.
/// Espaco é a largura do lado em que ela está, antes do recuo da cascata.
/// </summary>
public readonly record struct LugarDaFaixa(double X, double Largura, double Espaco);

/// <summary>
/// Reparte a largura da coluna do dia entre as faixas que coincidem no horário.
/// </summary>
public static class ColunaDoCalendario
{
    public const double Recuo = 16;
    public const double LarguraMinima = 88;

    /// <summary>
    /// As faixas chegam do fundo para a frente. As automáticas que coincidem ficam em cascata,
    /// cada uma recuada sobre a de trás. O registro manual que coincide com uma faixa automática
    /// fica ao lado dela, cada um numa metade, para um não cobrir o outro.
    /// </summary>
    public static IReadOnlyList<LugarDaFaixa> Distribuir(IReadOnlyList<FaixaNaColuna> doFundoParaAFrente, double largura)
    {
        ArgumentNullException.ThrowIfNull(doFundoParaAFrente);
        var total = doFundoParaAFrente.Count;
        var lados = new Lado[total];
        for (var i = 0; i < total; i++)
        {
            for (var j = 0; j < total; j++)
            {
                if (doFundoParaAFrente[i].Manual == doFundoParaAFrente[j].Manual
                    || !Coincidem(doFundoParaAFrente[i], doFundoParaAFrente[j]))
                    continue;

                lados[i] = doFundoParaAFrente[i].Manual ? Lado.Direita : Lado.Esquerda;
                break;
            }
        }

        var meio = Math.Round(largura / 2);
        var lugares = new LugarDaFaixa[total];
        for (var i = 0; i < total; i++)
        {
            var atras = 0;
            for (var j = 0; j < i; j++)
            {
                if (Coincidem(doFundoParaAFrente[j], doFundoParaAFrente[i]) && SeCobrem(lados[j], lados[i]))
                    atras++;
            }

            var comeco = lados[i] == Lado.Direita ? meio : 0;
            var espaco = lados[i] switch
            {
                Lado.Esquerda => meio,
                Lado.Direita => largura - meio,
                _ => largura,
            };
            var recuo = Math.Min(atras * Recuo, Math.Max(0, espaco - LarguraMinima));
            lugares[i] = new LugarDaFaixa(comeco + recuo, Math.Max(0, espaco - recuo), espaco);
        }

        return lugares;
    }

    private static bool Coincidem(FaixaNaColuna a, FaixaNaColuna b) => a.Inicio < b.Fim && b.Inicio < a.Fim;

    private static bool SeCobrem(Lado a, Lado b) => a == Lado.Inteira || b == Lado.Inteira || a == b;

    private enum Lado
    {
        Inteira,
        Esquerda,
        Direita,
    }
}
