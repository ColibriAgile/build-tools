using System.Text;
using BuildTools.Models;
using Spectre.Console;

namespace BuildTools.Resumos;

/// <summary>
/// Exibe relatório de deploy em formato Markdown.
/// </summary>
public sealed class ResumoDeployMarkdown : IResumo
{
    private readonly IAnsiConsole _console;
    private readonly DeployResultado _resultado;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ResumoDeployMarkdown"/>.
    /// </summary>
    /// <param name="console">Console para saída de informações.</param>
    /// <param name="resultado">Resultado do deploy.</param>
    public ResumoDeployMarkdown(IAnsiConsole console, DeployResultado resultado)
    {
        _console = console;
        _resultado = resultado;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public void ExibirRelatorio()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Relatório de Deploy");
        sb.AppendLine();
        sb.AppendLine("## Resumo");
        sb.AppendLine();
        sb.AppendLine($"- **Ambiente**: {_resultado.Ambiente}");
        sb.AppendLine($"- **URL Marketplace**: {_resultado.UrlMarketplace}");
        sb.AppendLine($"- **Simulado**: {(_resultado.Simulado ? "Sim" : "Não")}");
        sb.AppendLine($"- **Tempo de Execução**: {_resultado.TempoExecucao.TotalSeconds:F1}s");
        sb.AppendLine($"- **Arquivos Enviados**: {_resultado.ArquivosEnviados.Count}");
        sb.AppendLine($"- **Arquivos Ignorados**: {_resultado.ArquivosIgnorados.Count}");
        sb.AppendLine($"- **Arquivos com Falha**: {_resultado.ArquivosFalharam.Count}");

        ExibirArquivosEnviados(sb);
        ExibirArquivosIgnorados(sb);
        ExibirArquivosComFalha(sb);

        _console.WriteLine(sb.ToString());
    }

    private void ExibirArquivosComFalha(StringBuilder sb)
    {
        if (_resultado.ArquivosFalharam.Count <= 0)
            return;

        sb.AppendLine("## Arquivos com Falha");
        sb.AppendLine();

        foreach (var arquivo in _resultado.ArquivosFalharam)
            sb.AppendLine($"- **{arquivo.NomeArquivoS3}**: {arquivo.MensagemErro}");

        sb.AppendLine();
    }

    private void ExibirArquivosIgnorados(StringBuilder sb)
    {
        if (_resultado.ArquivosIgnorados.Count <= 0)
            return;

        sb.AppendLine("## Arquivos Ignorados");
        sb.AppendLine();

        foreach (var arquivo in _resultado.ArquivosIgnorados)
            sb.AppendLine($"- **{arquivo.NomeArquivoS3}**: {arquivo.MensagemErro}");

        sb.AppendLine();
    }

    private void ExibirArquivosEnviados(StringBuilder sb)
    {
        if (_resultado.ArquivosEnviados.Count <= 0)
            return;

        sb.AppendLine();
        sb.AppendLine("## Arquivos Enviados com Sucesso");
        sb.AppendLine();

        foreach (var arquivo in _resultado.ArquivosEnviados)
        {
            sb.AppendLine($"### {arquivo.NomeArquivoS3}");
            sb.AppendLine();
            sb.AppendLine($"- **Pacote**: {arquivo.Manifesto?.Nome}");
            sb.AppendLine($"- **Versão**: {arquivo.Manifesto?.Versao}");
            sb.AppendLine($"- **Desenvolvimento**: {(arquivo.Manifesto?.Develop == true ? "Sim" : "Não")}");

            if (!string.IsNullOrEmpty(arquivo.Manifesto?.SiglaEmpresa))
                sb.AppendLine($"- **Empresa**: {arquivo.Manifesto.SiglaEmpresa}");

            if (!string.IsNullOrEmpty(arquivo.UrlS3))
                sb.AppendLine($"- **URL S3**: [{arquivo.NomeArquivoS3}]({arquivo.UrlS3})");

            sb.AppendLine();
        }
    }
}
