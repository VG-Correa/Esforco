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

public sealed record ExclusaoDeCategoria(bool Pode, string? Aviso, int EmUso, long? TrocarPor);

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
        EscolhaAoExcluirCategoria? escolha)
    {
        ArgumentNullException.ThrowIfNull(categorias);
        ArgumentNullException.ThrowIfNull(vinculos);
        var lista = categorias.ToList();
        if (lista.All(item => item.Id != id))
            return new ExclusaoDeCategoria(false, "Essa categoria não existe mais.", 0, null);

        var emUso = vinculos.Count(vinculo => vinculo.CategoriaId == id);
        if (emUso == 0)
            return new ExclusaoDeCategoria(true, null, 0, null);

        if (escolha is null)
        {
            var quantos = emUso == 1 ? "1 vínculo" : $"{emUso} vínculos";
            return new ExclusaoDeCategoria(
                false,
                $"Esta categoria está em {quantos}. Escolha trocar por outra ou tirar a categoria deles.",
                emUso,
                null);
        }

        if (escolha.TrocarPor is long outra)
        {
            if (outra == id)
                return new ExclusaoDeCategoria(false, "Escolha uma categoria diferente da que será excluída.", emUso, null);
            if (lista.All(item => item.Id != outra))
                return new ExclusaoDeCategoria(false, "A categoria escolhida para a troca não existe mais.", emUso, null);
        }

        return new ExclusaoDeCategoria(true, null, emUso, escolha.TrocarPor);
    }
}
