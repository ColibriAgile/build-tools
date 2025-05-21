# Colibri BuildTools

![Build Status](https://github.com/ColibriAgile/BuildTools/actions/workflows/build.yml/badge.svg?branch=master)
![Testes](https://github.com/ColibriAgile/BuildTools/actions/workflows/testes.yml/badge.svg?branch=master)
[![codecov](https://codecov.io/gh/ColibriAgile/BuildTools/branch/master/graph/badge.svg)](https://codecov.io/gh/ColibriAgile/BuildTools)

## Índice

- [Funcionalidades](#funcionalidades)
- [Como usar](#como-usar)
- [Parâmetros do comando](#parâmetros-do-comando)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Testes](#testes)
- [Requisitos](#requisitos)
- [Contribuição](#contribuição)
- [Licença](#licença)

O Colibri BuildTools é uma ferramenta CLI moderna em C# .NET 9 para empacotamento de arquivos baseada em manifesto, inspirada no script Python `empacotar.py`. O projeto utiliza DI, System.CommandLine, System.IO.Abstractions, Spectre.Console e recursos modernos do C# 12+, com arquitetura modular e testável.

## Funcionalidades

- Empacotamento de arquivos conforme manifesto (suporte a padrões regex)
- Geração e atualização automática do manifesto
- Compactação ZIP nativa (System.IO.Compression)
- Parâmetros avançados de linha de comando
- Estrutura modular e testável
- Saída colorida e amigável via Spectre.Console

## Como usar

```sh
dotnet run --project BuildTools/BuildTools.csproj -- empacotar --pasta <origem> --saida <destino> [--senha <senha>] [--versao <versao>] [--develop]
```

## Parâmetros do comando

- `--pasta`, `-p`, `/pasta`: Pasta de origem dos arquivos (**obrigatório**)
- `--saida`, `-s`, `/saida`: Pasta de saída do pacote (**obrigatório**)
- `--senha`, `-se`, `/senha`: Senha do ZIP (opcional)
- `--versao`, `-v`, `/versao`: Versão do pacote (opcional, sobrescreve a do manifesto)
- `--develop`, `-d`, `/develop`: Marca o pacote como versão de desenvolvimento (gera a chave `develop` no manifesto)

## Estrutura do Projeto

- `Commands/` - Comandos CLI
- `Services/` - Serviços de negócio e utilitários
- `Models/` - Modelos de dados (manifesto, arquivos, etc)
- `Constants/` - Constantes globais do empacotador
- `Program.cs` - Bootstrap e configuração DI

## Testes

Recomenda-se criar testes unitários para os serviços e comandos utilizando xUnit, NUnit ou MSTest.

## Requisitos

- .NET 9 SDK
- Bibliotecas principais: System.CommandLine, System.IO.Abstractions, Spectre.Console, System.IO.Compression

## Contribuição

Pull requests são bem-vindos! Siga o style guide do projeto e mantenha a arquitetura modular.

## Licença

Este projeto está licenciado sob a licença MIT.
