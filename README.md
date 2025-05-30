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
- Deploy de pacotes .cmpkg para AWS S3 e notificação de marketplace
- Notificação direta do marketplace para pacotes específicos
- Geração e atualização automática do manifesto
- Compactação ZIP nativa (System.IO.Compression)
- Parâmetros avançados de linha de comando
- Opção de padronização de nomes de arquivos zip
- Resumo do empacotamento e deploy em console ou markdown
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

### Deploy de pacotes para AWS S3 e marketplace

```sh
BuildTools deploy <pasta> --ambiente <ambiente> --mkt-url <url> [--simulado] [--forcar] [--access-key <key>] [--secret-key <secret>] [--s3-region <regiao>] [--resumo <tipo>] [--silencioso] [--sem-cor]
```

### Notificar marketplace sobre pacote específico

```sh
BuildTools notificar-market <pasta> [--ambiente <ambiente>] [--mkt-url <url>] [--silencioso] [--sem-cor]
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

### Comando `deploy`

- `<pasta>`: Pasta contendo os arquivos .cmpkg para deploy (**obrigatório**)
- `--ambiente`, `-a`: Ambiente de destino: `desenvolvimento`, `producao` ou `stage` (**obrigatório**)
- `--mkt-url`, `-m`: URL do marketplace para notificação (**obrigatório**)
- `--simulado`, `-si`: Executa em modo simulação (não faz upload/notificação real) (opcional)
- `--forcar`, `-f`: Força o upload mesmo se o arquivo já existir no S3 (opcional)
- `--access-key`, `-ak`: Access Key da AWS (opcional, pode usar variável de ambiente AWS_ACCESS_KEY_ID)
- `--secret-key`, `-sk`: Secret Key da AWS (opcional, pode usar variável de ambiente AWS_SECRET_ACCESS_KEY)
- `--s3-region`, `-sr`: Região do bucket S3 (padrão: us-east-1) (opcional)
- `--resumo`: Exibe um resumo ao final do deploy. Valores possíveis: `nenhum`, `console`, `markdown` (opcional)
- `--silencioso`: Executa o comando em modo silencioso, sem mensagens de log (opcional)
- `--sem-cor`: Executa o comando sem cores (opcional)

### Comando `notificar-market`

- `<pasta>`: Pasta contendo o arquivo manifesto.dat (**obrigatório**)
- `--ambiente`, `-a`: Ambiente de destino: `desenvolvimento`, `stage` ou `producao` (padrão: desenvolvimento) (opcional)
- `--mkt-url`, `-m`: URL do marketplace para notificação (opcional, usa URL padrão do ambiente)
- `--silencioso`: Executa o comando em modo silencioso, sem mensagens de log (opcional)
- `--sem-cor`: Executa o comando sem cores (opcional)

## Detalhes do Comando Deploy

O comando `deploy` faz o upload de pacotes `.cmpkg` para AWS S3 e notifica um marketplace via API REST. Foi migrado do sistema Java s3-uploader original.

### Configuração AWS

As credenciais AWS podem ser fornecidas de duas formas:

1. **Parâmetros de linha de comando**: `--access-key` e `--secret-key`
2. **Variáveis de ambiente**: `AWS_ACCESS_KEY_ID` e `AWS_SECRET_ACCESS_KEY`

### Estrutura de Pastas no S3

- **desenvolvimento**: `packages-dev/`
- **stage**: `packages-stage/`
- **producao**: `packages/`

### Funcionamento

1. **Busca arquivos**: Localiza todos os arquivos `.cmpkg` na pasta especificada
2. **Lê manifestos**: Extrai informações de cada manifesto (nome, versão, etc.)
3. **Upload S3**: Faz upload para a pasta correspondente ao ambiente
4. **Notificação**: Envia dados para o marketplace via POST com autenticação JWT
5. **Relatório**: Exibe resumo dos sucessos e falhas

### Exemplos de Uso

```sh
# Deploy básico para produção
BuildTools deploy ./pacotes --ambiente producao --mkt-url https://marketplace.exemplo.com

# Deploy em modo simulação
BuildTools deploy ./pacotes --ambiente desenvolvimento --mkt-url https://dev.marketplace.com --simulado

# Deploy forçado com credenciais AWS específicas
BuildTools deploy ./pacotes --ambiente stage --mkt-url https://stage.marketplace.com --forcar --access-key AKIA... --secret-key xyz...
```

## Detalhes do Comando Notificar-Market

O comando `notificar-market` permite notificar o marketplace sobre um pacote específico sem fazer upload para S3. É útil para re-notificar pacotes já enviados ou quando o upload foi feito separadamente.

### Funcionamento da Notificação

1. **Lê manifesto**: Localiza e lê o arquivo `manifesto.dat` na pasta especificada
2. **Determina URL**: Usa URL customizada ou URL padrão do ambiente selecionado
3. **Notificação**: Envia dados do pacote para o marketplace via POST com autenticação JWT
4. **Feedback**: Exibe resultado da operação (sucesso ou falha)

### URLs Padrão por Ambiente

- **desenvolvimento**: `https://qa-marketplace.ncrcolibri.com.br`
- **stage**: `https://qa-marketplace.ncrcolibri.com.br`
- **producao**: `https://marketplace.ncrcolibri.com.br`

### Exemplos de Notificação

```sh
# Notificação simples para desenvolvimento (padrão)
BuildTools notificar-market ./pasta-com-manifesto

# Notificação para homologação
BuildTools notificar-market ./pasta-com-manifesto --ambiente homologacao

# Notificação com URL customizada
BuildTools notificar-market ./pasta-com-manifesto --mkt-url https://custom.marketplace.com

# Notificação silenciosa para produção
BuildTools notificar-market ./pasta-com-manifesto --ambiente producao --silencioso
```

## Estrutura do Projeto

- `Commands/` - Comandos CLI (`empacotar`, `empacotar_scripts`, `deploy`)
- `Services/` - Serviços de negócio e utilitários
- `Models/` - Modelos de dados (manifesto, arquivos, deploy, etc)
- `Constants/` - Constantes globais do empacotador
- `Resumos/` - Implementações de resumos para console e markdown
- `Program.cs` - Bootstrap e configuração DI

## Requisitos

- .NET 9 SDK
- Bibliotecas principais: System.CommandLine, System.IO.Abstractions, Spectre.Console, System.IO.Compression
- Para comando `deploy`: AWS SDK for .NET (AWSSDK.S3), System.IdentityModel.Tokens.Jwt

