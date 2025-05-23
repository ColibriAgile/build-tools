# Colibri BuildTools

[![Build master](https://github.com/ColibriAgile/build-tools/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/ColibriAgile/build-tools/actions/workflows/build.yml)
[![Build production](https://github.com/ColibriAgile/build-tools/actions/workflows/build-production.yml/badge.svg)](https://github.com/ColibriAgile/build-tools/actions/workflows/build-production.yml)
[![Testes](https://github.com/ColibriAgile/build-tools/actions/workflows/testes.yml/badge.svg?branch=master)](https://github.com/ColibriAgile/build-tools/actions/workflows/testes.yml)
[![codecov](https://codecov.io/gh/ColibriAgile/build-tools/graph/badge.svg?token=MKMMKLHHCS)](https://codecov.io/gh/ColibriAgile/build-tools)

## Índice

- [Funcionalidades](#funcionalidades)
- [Como usar](#como-usar)
- [Parâmetros dos comandos](#parâmetros-dos-comandos)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Requisitos](#requisitos)

O Colibri BuildTools é uma ferramenta CLI moderna em C# .NET 9 para empacotamento de arquivos baseada em manifesto, inspirada no script Python `empacotar.py`. O projeto utiliza DI, System.CommandLine, System.IO.Abstractions, Spectre.Console e recursos modernos do C# 12+, com arquitetura modular e testável.

## Funcionalidades

- Empacotamento de arquivos conforme manifesto (suporte a padrões regex)
- Empacotamento de scripts SQL em pacotes zip para inclusão em .cmpkg
- Geração e atualização automática do manifesto
- Compactação ZIP nativa (System.IO.Compression)
- Parâmetros avançados de linha de comando
- Opção de padronização de nomes de arquivos zip
- Resumo do empacotamento em console ou markdown
- Estrutura modular e testável
- Saída colorida e amigável via Spectre.Console
- Suporte a execução silenciosa e sem cor

## Como usar

### Empacotar arquivos conforme manifesto

```sh
BuildTools empacotar --pasta <origem> --saida <destino> [--senha <senha>] [--versao <versao>] [--develop] [--resumo <tipo>] [--silencioso] [--sem-cor]
```

### Empacotar scripts SQL em pacotes zip

```sh
BuildTools empacotar_scripts --pasta <origem> --saida <destino> [--padronizar_nomes <true|false>] [--resumo <tipo>] [--silencioso] [--sem-cor]
```

## Parâmetros dos comandos

### Comando `empacotar`

- `--pasta`, `-p`: Pasta de origem dos arquivos (**obrigatório**)
- `--saida`, `-s`: Pasta de saída do pacote (**obrigatório**)
- `--senha`, `-se`: Senha do ZIP (opcional)
- `--versao`, `-v`: Versão do pacote (opcional, sobrescreve a do manifesto)
- `--develop`, `-d`: Marca o pacote como versão de desenvolvimento (gera a chave `develop` no manifesto)
- `--resumo`: Exibe um resumo ao final do empacotamento. Valores possíveis: `nenhum`, `console`, `markdown` (opcional)
- `--silencioso`: Executa o comando em modo silencioso, sem mensagens de log (opcional)
- `--sem-cor`: Executa o comando sem cores (opcional)

### Comando `empacotar_scripts`

- `--pasta`, `-p`: Pasta de origem dos scripts (**obrigatório**)
- `--saida`, `-s`: Pasta de saída dos pacotes zip (**obrigatório**)
- `--padronizar_nomes`, `-pn`: Padroniza o nome dos arquivos zip gerados conforme regex (padrão: true)
- `--resumo`: Exibe um resumo ao final do empacotamento. Valores possíveis: `nenhum`, `console`, `markdown` (opcional)
- `--silencioso`: Executa o comando em modo silencioso, sem mensagens de log (opcional)
- `--sem-cor`: Executa o comando sem cores (opcional)

## Estrutura do Projeto

- `Commands/` - Comandos CLI (`empacotar`, `empacotar_scripts`)
- `Services/` - Serviços de negócio e utilitários
- `Models/` - Modelos de dados (manifesto, arquivos, etc)
- `Constants/` - Constantes globais do empacotador
- `Program.cs` - Bootstrap e configuração DI

## Requisitos

- .NET 9 SDK
- Bibliotecas principais: System.CommandLine, System.IO.Abstractions, Spectre.Console, System.IO.Compression

