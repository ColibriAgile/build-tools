namespace BuildTools.Resumos;

/// <summary>
/// Interface que define o contrato para exibir resumos de execuções de comandos.
/// </summary>
public interface IResumo
{
    /// <summary>
    /// Exibe o relatório para o tipo de resultado especificado.
    /// </summary>
    void ExibirRelatorio();
}
