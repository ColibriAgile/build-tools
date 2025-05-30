using System.CommandLine;

namespace BuildTools.Validation;

/// <summary>
/// Validador para ambientes de deploy.
/// </summary>
public static class AmbienteValidator
{
    private static readonly string[] _ambientesValidos = ["desenvolvimento", "producao", "stage"];

    /// <summary>
    /// Valida se o ambiente informado é válido.
    /// </summary>
    /// <param name="ambiente">Ambiente a ser validado.</param>
    /// <exception cref="ArgumentException">Lançada quando o ambiente é inválido.</exception>
    public static void ValidarAmbiente(string ambiente)
    {
        if (string.IsNullOrWhiteSpace(ambiente) || !_ambientesValidos.Contains(ambiente, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Ambiente '{ambiente}' inválido. Valores permitidos: {string.Join(", ", _ambientesValidos)}");
    }

    /// <summary>
    /// Cria uma opção de ambiente pré-configurada com validação.
    /// </summary>
    /// <param name="valorPadrao">Valor padrão para a opção.</param>
    /// <returns>Opção configurada com validação.</returns>
    public static Option<string> CriarOpcaoAmbiente(string valorPadrao = "desenvolvimento")
    {
        var opcao = new Option<string>
        (
            aliases: ["--ambiente", "-a"],
            description: "Ambiente de deploy (\"desenvolvimento\", \"producao\" ou \"stage\")"
        )
        {
            IsRequired = false
        };

        opcao.SetDefaultValue(valorPadrao);

        opcao.AddValidator
        (
            result =>
            {
                var valor = result.GetValueForOption(opcao);

                if (string.IsNullOrEmpty(valor))
                    return;

                if (!_ambientesValidos.Contains(valor, StringComparer.OrdinalIgnoreCase))
                    result.ErrorMessage = $"Ambiente '{valor}' inválido. Valores permitidos: {string.Join(", ", _ambientesValidos)}";
            }
        );

        return opcao;
    }

    /// <summary>
    /// Obtém a lista de ambientes válidos.
    /// </summary>
    /// <returns>Array com os ambientes válidos.</returns>
    public static string[] ObterAmbientesValidos()
        => [.. _ambientesValidos];
}
