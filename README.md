# Colibri BuildTools

O Colibri BuildTools é uma ferramenta CLI moderna em C# .NET 9 para empacotamento de arquivos baseada em manifesto, inspirada no script Python `empacotar.py`. O projeto utiliza DI, System.CommandLine, System.IO.Abstractions e Spectre.Console, com arquitetura modular e testável.

## Funcionalidades
- Empacotamento de arquivos conforme manifesto
- Geração e atualização automática do manifesto
- Compactação ZIP nativa (System.IO.Compression)
- Estrutura modular e testável
- Saída colorida e amigável via Spectre.Console

## Como usar

```sh
dotnet run --project BuildTools/BuildTools.csproj -- empacotar --pasta <origem> --saida <destino> [--senha <senha>]
```

- `--pasta` (ou `-p`): Pasta de origem dos arquivos
- `--saida` (ou `-s`): Pasta de saída do pacote
- `--senha` (ou `-se`): Senha do ZIP (opcional, ignorada na versão atual)

## Estrutura do Projeto

- `Commands/` - Comandos CLI
- `Services/` - Serviços de negócio e utilitários
- `Program.cs` - Bootstrap e configuração DI

## Testes
Recomenda-se criar testes unitários para os serviços e comandos utilizando xUnit, NUnit ou MSTest.

## Requisitos
- .NET 9 SDK

## Contribuição
Pull requests são bem-vindos! Siga o style guide do projeto e mantenha a arquitetura modular.

## Licença
Este projeto está licenciado sob a licença MIT.
