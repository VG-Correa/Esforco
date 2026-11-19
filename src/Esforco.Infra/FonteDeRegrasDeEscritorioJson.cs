using System.Text;
using System.Text.Json;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Infra;

public sealed class FonteDeRegrasDeEscritorioJson : IFonteDeRegrasDeEscritorio
{
    public const string NomeDoArquivo = "regras-escritorio.json";

    public const string TextoModelo = """
        {
          "comentario": "O escritório fica vazio até uma regra casar o título da janela. Coloque as regras em 'regras'. O campo 'exemplo' não é lido; ele só mostra o formato. Salve o arquivo: a próxima leitura já usa a alteração.",
          "exemplo": {
            "escritorio": "Nome do escritório",
            "contem": ["trecho que aparece no título"]
          },
          "regras": []
        }
        """;

    private static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _caminho;
    private readonly object _trava = new();
    private IReadOnlyList<RegraDeEscritorio> _cache = [];
    private DateTime _gravadoEm;
    private bool _carregado;

    public FonteDeRegrasDeEscritorioJson(string caminho)
    {
        _caminho = caminho;
    }

    public static string CaminhoAoLadoDoExecutavel() =>
        Path.Combine(PastaDoPrograma.Obter(), NomeDoArquivo);

    public IReadOnlyList<RegraDeEscritorio> Carregar()
    {
        lock (_trava)
        {
            try
            {
                if (!File.Exists(_caminho))
                {
                    var pasta = Path.GetDirectoryName(_caminho);
                    if (!string.IsNullOrWhiteSpace(pasta))
                        Directory.CreateDirectory(pasta);

                    File.WriteAllText(_caminho, TextoModelo, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    _cache = [];
                    _gravadoEm = File.GetLastWriteTimeUtc(_caminho);
                    _carregado = true;
                    return _cache;
                }

                var gravadoEm = File.GetLastWriteTimeUtc(_caminho);
                if (_carregado && gravadoEm == _gravadoEm)
                    return _cache;

                var novas = Interpretar(File.ReadAllText(_caminho));
                _cache = novas;
                _gravadoEm = gravadoEm;
                _carregado = true;
                return _cache;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                return _cache;
            }
        }
    }

    private static IReadOnlyList<RegraDeEscritorio> Interpretar(string json)
    {
        var documento = JsonSerializer.Deserialize<DocumentoDto>(json, Opcoes);
        if (documento?.Regras is null || documento.Regras.Count == 0)
            return [];

        var regras = new List<RegraDeEscritorio>();
        foreach (var regra in documento.Regras)
        {
            if (regra is null)
                continue;

            regras.Add(new RegraDeEscritorio(
                regra.Escritorio ?? "",
                regra.Contem ?? []));
        }

        return regras;
    }

    private sealed class DocumentoDto
    {
        public List<RegraDto>? Regras { get; set; }
    }

    private sealed class RegraDto
    {
        public string? Escritorio { get; set; }

        public List<string>? Contem { get; set; }
    }
}
