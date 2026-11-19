namespace Esforco.Dominio;

public sealed record NoDeEsforco(
    string Rotulo,
    IReadOnlyList<NoDeEsforco> Filhos,
    IReadOnlyList<IntervaloDeEsforco> Intervalos,
    TimeSpan DuracaoTotal);

public static class AgrupadorDeEsforco
{
    public static IReadOnlyList<NoDeEsforco> Agrupar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        DateTimeOffset agora,
        NomesDePainel? nomes = null)
    {
        ArgumentNullException.ThrowIfNull(intervalos);

        var raiz = new NoMutavel("");
        foreach (var intervalo in intervalos)
            raiz.Inserir(CaminhoDeEsforco.De(intervalo, agora, nomes), 0, intervalo);

        return raiz.Filhos
            .Select(filho => filho.Concluir(agora))
            .OrderByDescending(Recente)
            .ToList();
    }

    private static DateTimeOffset Recente(NoDeEsforco no)
    {
        var maximo = DateTimeOffset.MinValue;
        foreach (var intervalo in no.Intervalos)
        {
            if (intervalo.Inicio > maximo)
                maximo = intervalo.Inicio;
        }

        foreach (var filho in no.Filhos)
        {
            var recente = Recente(filho);
            if (recente > maximo)
                maximo = recente;
        }

        return maximo;
    }

    private static TimeSpan Somar(IEnumerable<IntervaloDeEsforco> intervalos, DateTimeOffset agora)
    {
        var total = TimeSpan.Zero;
        foreach (var intervalo in intervalos)
            total += intervalo.DuracaoAte(agora);
        return total;
    }

    private sealed class NoMutavel
    {
        public NoMutavel(string rotulo) => Rotulo = rotulo;

        public string Rotulo { get; }

        public List<NoMutavel> Filhos { get; } = [];

        public List<IntervaloDeEsforco> Intervalos { get; } = [];

        public void Inserir(IReadOnlyList<string> caminho, int indice, IntervaloDeEsforco intervalo)
        {
            if (indice >= caminho.Count)
            {
                Intervalos.Add(intervalo);
                return;
            }

            var rotulo = caminho[indice];
            var filho = Filhos.FirstOrDefault(item => item.Rotulo == rotulo);
            if (filho is null)
            {
                filho = new NoMutavel(rotulo);
                Filhos.Add(filho);
            }

            filho.Inserir(caminho, indice + 1, intervalo);
        }

        public NoDeEsforco Concluir(DateTimeOffset agora)
        {
            var filhos = Filhos
                .Select(filho => filho.Concluir(agora))
                .OrderByDescending(Recente)
                .ToList();
            var linhas = Intervalos.OrderByDescending(intervalo => intervalo.Inicio).ToList();
            var duracao = Somar(linhas, agora);
            foreach (var filho in filhos)
                duracao += filho.DuracaoTotal;
            return new NoDeEsforco(Rotulo, filhos, linhas, duracao);
        }
    }
}
