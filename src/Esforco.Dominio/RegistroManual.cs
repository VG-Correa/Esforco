using System.Globalization;

namespace Esforco.Dominio;

/// <summary>
/// Tempo lançado à mão para um escritório. Não vem da leitura de nenhuma janela e fica em tabela própria.
/// </summary>
public sealed record RegistroManual(
    long Id,
    string Escritorio,
    long? CategoriaId,
    string Descricao,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    DateTimeOffset CriadoEm,
    DateTimeOffset AlteradoEm)
{
    public const string NomeDoPrograma = "Registro manual";

    public const string SemDescricao = "Sem descrição";
}

public sealed record PedidoDeRegistroManual(
    string Escritorio,
    string Categoria,
    string Descricao,
    DateTimeOffset Inicio,
    DateTimeOffset Fim);

/// <summary>
/// O que o intervalo montado a partir de um registro manual carrega dele, para as telas.
/// </summary>
public sealed record MarcaDeRegistroManual(long Id, long? CategoriaId, string Categoria, string Descricao);

public sealed record PeriodoDeRegistro(DateTimeOffset? Inicio, DateTimeOffset? Fim, string? Aviso)
{
    public bool Valido => Aviso is null && Inicio is not null && Fim is not null;
}

public sealed record CorteDeRegistrosManuais(
    IReadOnlyList<long> Excluir,
    IReadOnlyList<RegistroManual> Atualizar,
    IReadOnlyList<RegistroManual> Criar);

public static class CadastroDeRegistrosManuais
{
    public static readonly TimeSpan MaiorDuracao = TimeSpan.FromHours(24);

    public const int MaiorDescricao = 200;

    public static string? Impedir(PedidoDeRegistroManual pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        if ((pedido.Escritorio ?? "").Trim().Length == 0)
            return "Escolha o escritório.";

        if (pedido.Fim <= pedido.Inicio)
            return "O fim precisa ser depois do início.";

        if (pedido.Fim - pedido.Inicio > MaiorDuracao)
            return "Um registro manual tem até 24 horas.";

        return NomesDePainel.Exibir(pedido.Descricao).Length > MaiorDescricao
            ? $"A descrição tem até {MaiorDescricao} letras."
            : null;
    }

    /// <summary>
    /// Junta o dia e as horas escritas na tela. Fim igual ou antes do início só é aceito
    /// quando a pessoa diz que o registro termina no dia seguinte.
    /// </summary>
    public static PeriodoDeRegistro LerPeriodo(DateTime? dia, string? inicio, string? fim, bool terminaNoDiaSeguinte)
    {
        if (dia is not DateTime data)
            return new PeriodoDeRegistro(null, null, "Escolha o dia.");

        if (HoraDoDia.Ler(inicio) is not TimeSpan comeco)
            return new PeriodoDeRegistro(null, null, "Escreva o início como 14:30.");

        if (HoraDoDia.Ler(fim) is not TimeSpan termino)
            return new PeriodoDeRegistro(null, null, "Escreva o fim como 15:00.");

        var de = Local(data.Date + comeco);
        var ate = Local(data.Date.AddDays(terminaNoDiaSeguinte ? 1 : 0) + termino);
        if (ate <= de)
        {
            return new PeriodoDeRegistro(
                de,
                ate,
                "O fim está antes do início. Se o registro passa da meia-noite, marque Termina no dia seguinte.");
        }

        return ate - de > MaiorDuracao
            ? new PeriodoDeRegistro(de, ate, "Um registro manual tem até 24 horas.")
            : new PeriodoDeRegistro(de, ate, null);
    }

    /// <summary>
    /// O que a tela avisa sem impedir: registro que ainda não aconteceu e registro que passa da meia-noite.
    /// </summary>
    public static IReadOnlyList<string> Notas(DateTimeOffset inicio, DateTimeOffset fim, DateTimeOffset agora)
    {
        var notas = new List<string>();
        if (inicio.ToLocalTime().Date != fim.AddTicks(-1).ToLocalTime().Date)
            notas.Add("Passa da meia-noite: no Registro e em Escritórios o tempo todo fica no dia em que começa.");

        if (inicio >= agora)
            notas.Add("Esse horário ainda não chegou. O tempo entra nos totais assim que for salvo.");
        else if (fim > agora)
            notas.Add("O fim ainda não chegou. O tempo inteiro entra nos totais assim que for salvo.");

        return notas;
    }

    public static string Duracao(TimeSpan duracao)
    {
        var minutos = (int)Math.Round(duracao.TotalMinutes);
        if (minutos < 60)
            return $"{minutos} min";

        return minutos % 60 == 0 ? $"{minutos / 60} h" : $"{minutos / 60} h {minutos % 60} min";
    }

    /// <summary>
    /// O que fazer com os registros manuais quando um período é zerado: o que cabe inteiro sai,
    /// o que passa de um lado é encurtado e o que passa dos dois vira dois.
    /// </summary>
    public static CorteDeRegistrosManuais Cortar(
        IEnumerable<RegistroManual> registros,
        DateTimeOffset inicio,
        DateTimeOffset fim)
    {
        ArgumentNullException.ThrowIfNull(registros);
        if (fim <= inicio)
            throw new ArgumentOutOfRangeException(nameof(fim), "O fim do período precisa ser depois do início.");

        var excluir = new List<long>();
        var atualizar = new List<RegistroManual>();
        var criar = new List<RegistroManual>();
        foreach (var registro in registros)
        {
            if (registro.Inicio >= fim || registro.Fim <= inicio)
                continue;

            var comecaAntes = registro.Inicio < inicio;
            var terminaDepois = registro.Fim > fim;
            if (!comecaAntes && !terminaDepois)
            {
                excluir.Add(registro.Id);
                continue;
            }

            if (comecaAntes && terminaDepois)
            {
                atualizar.Add(registro with { Fim = inicio });
                criar.Add(registro with { Id = 0, Inicio = fim });
                continue;
            }

            atualizar.Add(comecaAntes ? registro with { Fim = inicio } : registro with { Inicio = fim });
        }

        return new CorteDeRegistrosManuais(excluir, atualizar, criar);
    }

    private static DateTimeOffset Local(DateTime instante)
    {
        var local = DateTime.SpecifyKind(instante, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
    }
}

/// <summary>
/// Lê a hora escrita à mão: 9, 09, 9:30, 9h30, 9h, 930 ou 0930.
/// </summary>
public static class HoraDoDia
{
    public static TimeSpan? Ler(string? texto)
    {
        var limpo = (texto ?? "").Trim().ToLowerInvariant().Replace('h', ':').Replace('.', ':');
        if (limpo.EndsWith(':'))
            limpo = limpo[..^1];
        if (limpo.Length == 0)
            return null;

        string horas;
        string minutos;
        var marca = limpo.IndexOf(':');
        if (marca >= 0)
        {
            horas = limpo[..marca];
            minutos = limpo[(marca + 1)..];
            if (horas.Length is < 1 or > 2 || minutos.Length != 2)
                return null;
        }
        else if (limpo.Length <= 2)
        {
            horas = limpo;
            minutos = "00";
        }
        else if (limpo.Length <= 4)
        {
            horas = limpo[..^2];
            minutos = limpo[^2..];
        }
        else
        {
            return null;
        }

        if (!int.TryParse(horas, NumberStyles.None, CultureInfo.InvariantCulture, out var hora)
            || !int.TryParse(minutos, NumberStyles.None, CultureInfo.InvariantCulture, out var minuto)
            || hora > 23
            || minuto > 59)
        {
            return null;
        }

        return new TimeSpan(hora, minuto, 0);
    }

    public static string Escrever(int minutoDoDia)
    {
        var minuto = ((minutoDoDia % 1440) + 1440) % 1440;
        return string.Create(CultureInfo.InvariantCulture, $"{minuto / 60:00}:{minuto % 60:00}");
    }
}

/// <summary>
/// O horário que sai do clique e do arrasto na coluna do dia, e o que o botão de novo registro sugere.
/// </summary>
public static class SelecaoDeHorario
{
    public const int PassoDoClique = 15;

    public const int PassoDoArrasto = 5;

    public const int DuracaoDoClique = 30;

    private const int FimDoDia = 24 * 60;

    public static (int Inicio, int Fim) DoClique(double minuto)
    {
        var inicio = (int)Math.Floor(Limitar(minuto) / PassoDoClique) * PassoDoClique;
        if (inicio > FimDoDia - DuracaoDoClique)
            inicio = FimDoDia - DuracaoDoClique;
        return (inicio, inicio + DuracaoDoClique);
    }

    public static (int Inicio, int Fim) DoArrasto(double de, double ate)
    {
        if (ate < de)
            (de, ate) = (ate, de);

        var inicio = Arredondar(Limitar(de));
        var fim = Arredondar(Limitar(ate));
        if (fim - inicio < PassoDoArrasto)
        {
            if (inicio > FimDoDia - PassoDoArrasto)
                inicio = FimDoDia - PassoDoArrasto;
            fim = inicio + PassoDoArrasto;
        }

        return (inicio, fim);
    }

    /// <summary>
    /// Para hoje, a meia hora que acabou de passar. Para outro dia, 9:00 às 9:30.
    /// </summary>
    public static (int Inicio, int Fim) Sugerir(DateTime dia, DateTime agora)
    {
        if (dia.Date != agora.Date)
            return (9 * 60, 9 * 60 + DuracaoDoClique);

        var fim = (agora.Hour * 60 + agora.Minute) / PassoDoArrasto * PassoDoArrasto;
        return fim < DuracaoDoClique ? (0, DuracaoDoClique) : (fim - DuracaoDoClique, fim);
    }

    private static double Limitar(double minuto) =>
        double.IsNaN(minuto) ? 0 : Math.Clamp(minuto, 0, FimDoDia);

    private static int Arredondar(double minuto) =>
        (int)Math.Round(minuto / PassoDoArrasto, MidpointRounding.AwayFromZero) * PassoDoArrasto;
}
