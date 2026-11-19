namespace Esforco.Dominio;

public sealed record AmostraDeJanela(
    string Programa,
    string Titulo,
    bool Minimizada,
    string Endereco = "",
    string Conversa = "",
    bool Propria = false);
