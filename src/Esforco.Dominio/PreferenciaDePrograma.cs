namespace Esforco.Dominio;

public sealed record PreferenciaDePrograma(string Programa, bool Oculto, bool Fixado, bool Pausado)
{
    public static PreferenciaDePrograma Vazia(string programa) => new(programa, false, false, false);

    public bool TemAlguma => Oculto || Fixado || Pausado;
}
