namespace Esforco.Aplicacao;

public interface IRelogio
{
    DateTimeOffset Agora();
}

public sealed class RelogioDoSistema : IRelogio
{
    public DateTimeOffset Agora() => DateTimeOffset.Now;
}
