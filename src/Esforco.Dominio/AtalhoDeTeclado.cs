namespace Esforco.Dominio;

/// <summary>
/// Combinação de teclas que abre a janelinha de vínculo. Tecla é uma letra, um dígito ou F1 a F12.
/// </summary>
public sealed record AtalhoDeTeclado(bool Ctrl, bool Alt, bool Shift, bool Win, string Tecla)
{
    public static AtalhoDeTeclado Padrao { get; } = new(true, true, false, false, "E");

    public override string ToString()
    {
        var partes = new List<string>(5);
        if (Ctrl)
            partes.Add("Ctrl");
        if (Alt)
            partes.Add("Alt");
        if (Shift)
            partes.Add("Shift");
        if (Win)
            partes.Add("Win");
        partes.Add(Tecla);
        return string.Join("+", partes);
    }

    /// <summary>
    /// Código da tecla para o Windows: letras e dígitos usam o próprio caractere; F1 começa em 0x70.
    /// </summary>
    public int Codigo
    {
        get
        {
            if (Tecla.Length == 1)
                return Tecla[0];
            return 0x70 + int.Parse(Tecla.AsSpan(1), System.Globalization.CultureInfo.InvariantCulture) - 1;
        }
    }

    public static string? Impedir(bool ctrl, bool alt, bool win, string? tecla)
    {
        if (!ctrl && !alt && !win)
            return "Use Ctrl, Alt ou Win junto com a tecla.";
        if (!TeclaValida(Limpar(tecla)))
            return "Escolha uma letra, um número ou uma tecla de F1 a F12.";
        return null;
    }

    public static AtalhoDeTeclado? Criar(bool ctrl, bool alt, bool shift, bool win, string? tecla) =>
        Impedir(ctrl, alt, win, tecla) is null
            ? new AtalhoDeTeclado(ctrl, alt, shift, win, Limpar(tecla))
            : null;

    public static AtalhoDeTeclado? Ler(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        bool ctrl = false, alt = false, shift = false, win = false;
        string? tecla = null;
        foreach (var parte in texto.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (parte.ToUpperInvariant())
            {
                case "CTRL":
                    ctrl = true;
                    break;
                case "ALT":
                    alt = true;
                    break;
                case "SHIFT":
                    shift = true;
                    break;
                case "WIN":
                    win = true;
                    break;
                default:
                    if (tecla is not null)
                        return null;
                    tecla = parte;
                    break;
            }
        }

        return Criar(ctrl, alt, shift, win, tecla);
    }

    private static string Limpar(string? tecla) => (tecla ?? "").Trim().ToUpperInvariant();

    private static bool TeclaValida(string tecla)
    {
        if (tecla.Length == 1)
            return tecla[0] is (>= 'A' and <= 'Z') or (>= '0' and <= '9');

        return tecla.Length is 2 or 3
            && tecla[0] == 'F'
            && int.TryParse(tecla.AsSpan(1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var numero)
            && numero is >= 1 and <= 12;
    }
}
