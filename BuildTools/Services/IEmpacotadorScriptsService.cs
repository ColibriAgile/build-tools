﻿using BuildTools.Models;

namespace BuildTools.Services;

/// <summary>
/// Serviço para empacotamento de scripts SQL conforme regras do empacotar_scripts.py.
/// </summary>
public interface IEmpacotadorScriptsService
{
    /// <summary>
    /// Empacota o conteúdo da pasta especificada de scripts em um arquivo .zip e retorna o resultado.
    /// </summary>
    /// <remarks>Use este método para empacotar arquivos de uma pasta especificada em um único arquivo de saída. Certifique-se
    /// de que a pasta de entrada e o caminho de saída sejam válidos e acessíveis. O comportamento do método pode ser customizado
    /// utilizando os parâmetros padronizarNomes e silencioso.</remarks>
    /// <param name="pasta">O caminho para a pasta contendo os arquivos a serem empacotados. Não pode ser nulo ou vazio.</param>
    /// <param name="saida">O caminho de saída onde o arquivo empacotado será salvo. Não pode ser nulo ou vazio.</param>
    /// <param name="padronizarNomes">Indica se os nomes dos arquivos devem ser padronizados durante o empacotamento. true para padronizar;
    /// caso contrário, false.</param>
    /// <param name="silencioso">Indica se as mensagens de saída devem ser suprimidas durante o empacotamento. true para suprimir mensagens;
    /// caso contrário, false.</param>
    /// <returns>Um objeto EmpacotamentoScriptResultado contendo o resultado da operação de empacotamento.</returns>
    EmpacotamentoScriptResultado Empacotar(string pasta, string saida, bool padronizarNomes, bool silencioso);
}