namespace Esforco.Dominio;

public sealed record Categoria(long Id, string Nome);

/// <summary>
/// O que fazer com os vínculos de uma categoria excluída: trocar por outra ou ficar sem categoria.
/// </summary>
public sealed record EscolhaAoExcluirCategoria(long? TrocarPor)
{
    public static EscolhaAoExcluirCategoria Retirar { get; } = new((long?)null);

    public static EscolhaAoExcluirCategoria Trocar(long outra) => new(outra);
}

/// <summary>
/// EmUso soma os vínculos e os registros manuais da categoria. Onde diz isso por extenso, para a tela.
/// </summary>
public sealed record ExclusaoDeCategoria(bool Pode, string? Aviso, int EmUso, long? TrocarPor, string Onde = "");

public static class CadastroDeCategorias
{
    public const string SemCategoria = "Sem categoria";

    public static string? ImpedirNome(string? nome, IEnumerable<Categoria> existentes, long? ignorar = null)
    {
        ArgumentNullException.ThrowIfNull(existentes);
        var normal = NomesDePainel.Normalizar(nome);
        if (normal.Length == 0)
            return "Escreva o nome da categoria.";

        if (NomesDePainel.Exibir(nome).Length > 60)
            return "O nome da categoria tem até 60 letras.";

        foreach (var existente in existentes)
        {
            if (existente.Id != ignorar && NomesDePainel.Normalizar(existente.Nome) == normal)
                return "Essa categoria já existe.";
        }

        return null;
    }

    public static Categoria? ComNome(string? nome, IEnumerable<Categoria> existentes)
    {
        ArgumentNullException.ThrowIfNull(existentes);
        var normal = NomesDePainel.Normalizar(nome);
        if (normal.Length == 0)
            return null;

        return existentes.FirstOrDefault(item => NomesDePainel.Normalizar(item.Nome) == normal);
    }

    public static ExclusaoDeCategoria PlanejarExclusao(
        long id,
        IEnumerable<Categoria> categorias,
        IEnumerable<VinculoDeEscritorio> vinculos,
        EscolhaAoExcluirCategoria? escolha,
        IEnumerable<RegistroManual>? registros = null)
    {
        ArgumentNullException.ThrowIfNull(categorias);
        ArgumentNullException.ThrowIfNull(vinculos);
        var lista = categorias.ToList();
        if (lista.All(item => item.Id != id))
            return new ExclusaoDeCategoria(false, "Essa categoria não existe mais.", 0, null);

        var emVinculos = vinculos.Count(vinculo => vinculo.CategoriaId == id);
        var emRegistros = registros?.Count(registro => registro.CategoriaId == id) ?? 0;
        var emUso = emVinculos + emRegistros;
        if (emUso == 0)
            return new ExclusaoDeCategoria(true, null, 0, null);

        var onde = Onde(emVinculos, emRegistros);
        if (escolha is null)
        {
            return new ExclusaoDeCategoria(
                false,
                $"Esta categoria está em {onde}. Escolha trocar por outra ou tirar a categoria deles.",
                emUso,
                null,
                onde);
        }

        if (escolha.TrocarPor is long outra)
        {
            if (outra == id)
                return new ExclusaoDeCategoria(false, "Escolha uma categoria diferente da que será excluída.", emUso, null, onde);
            if (lista.All(item => item.Id != outra))
                return new ExclusaoDeCategoria(false, "A categoria escolhida para a troca não existe mais.", emUso, null, onde);
        }

        return new ExclusaoDeCategoria(true, null, emUso, escolha.TrocarPor, onde);
    }

    private static string Onde(int vinculos, int registros)
    {
        var partes = new List<string>();
        if (vinculos > 0)
            partes.Add(vinculos == 1 ? "1 vínculo" : $"{vinculos} vínculos");
        if (registros > 0)
            partes.Add(registros == 1 ? "1 registro manual" : $"{registros} registros manuais");
        return string.Join(" e ", partes);
    }
}
