namespace Esforco.Dominio;

public enum TipoDeItem
{
    Nenhum = 0,
    Aba = 1,
    Dashboard = 2,
}

public sealed record LocalDoPainel(
    string Escritorio,
    string Plano,
    string Periodo,
    string Sessao,
    string Item,
    TipoDeItem TipoDoItem);
