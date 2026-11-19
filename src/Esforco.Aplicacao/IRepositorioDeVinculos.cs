using Esforco.Dominio;

namespace Esforco.Aplicacao;

/// <summary>
/// Guarda categorias, vínculos manuais, registros manuais e o endereço das abas vinculadas, em tabelas próprias.
/// As tabelas que já existiam não são tocadas.
/// </summary>
public interface IRepositorioDeVinculos
{
    IReadOnlyList<Categoria> ListarCategorias();

    Categoria AdicionarCategoria(string nome);

    void RenomearCategoria(long id, string nome);

    /// <summary>
    /// Exclui a categoria e, na mesma operação, troca a categoria dos vínculos e dos registros manuais
    /// que a usavam por trocarPor, ou deixa esses sem categoria quando trocarPor é nulo.
    /// </summary>
    void ExcluirCategoria(long id, long? trocarPor);

    IReadOnlyList<RegistroManual> ListarRegistrosManuais();

    RegistroManual AdicionarRegistroManual(RegistroManual registro);

    void AtualizarRegistroManual(RegistroManual registro);

    void ExcluirRegistroManual(long id);

    IReadOnlyList<VinculoDeEscritorio> ListarVinculos();

    VinculoDeEscritorio AdicionarVinculo(VinculoDeEscritorio vinculo);

    void AtualizarVinculo(VinculoDeEscritorio vinculo);

    void ExcluirVinculo(long id);

    IReadOnlyDictionary<long, string> ListarEnderecos();

    void GuardarEndereco(long intervalo, string endereco);

    /// <summary>
    /// Apaga os endereços guardados de intervalos que não existem mais.
    /// </summary>
    void ManterEnderecos(IReadOnlyCollection<long> intervalos);

    string? LerConfiguracao(string chave);

    void GravarConfiguracao(string chave, string valor);

    IReadOnlyList<ContagemDeJanela> ListarContagens();

    /// <summary>
    /// Com Id zero, cria a regra de contagem. Com Id, troca o modo da que já existe.
    /// </summary>
    ContagemDeJanela SalvarContagem(ContagemDeJanela contagem);

    void ExcluirContagem(long id);
}
