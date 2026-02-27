# Changelog

Todas as alterações relevantes deste projeto serão documentadas neste arquivo.
O formato segue o padrão [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).

---

## [1.1.0] - 2026-02-27

### Adicionado
- **Detecção de primeira execução**: O bot identifica quando não há histórico anterior e ignora o envio de notificações, prevenindo spam de milhares de "novos usuários" na inicialização inicial.
- **`SlackBotConfiguration.cs`**: Nova classe de configuração fortemente tipada com `SlackToken`, `SlackCookie`, `SlackTeamId` e classe aninhada `SlackWebhooks` (`ChoroChannel`, `PrivateChannel`).
- **User Secrets**: Integração com `Microsoft.Extensions.Configuration.UserSecrets` para armazenamento seguro de credenciais fora do código-fonte.
- **`appsettings.template.json`**: Template público de configuração com instruções inline para novos desenvolvedores.
- **`SECURITY_CONFIG.md`**: Guia completo de configuração segura com instruções passo a passo para uso dos User Secrets.
- **Suporte a emojis no Windows**: `Console.OutputEncoding = Encoding.UTF8` para exibição correta de emojis no console do Windows.
- **Logs de performance**: `Stopwatch` integrado para medir o tempo de cada operação do fluxo principal.
- **Sistema de retry robusto**: Método `SendWithRetryAsync` com suporte a múltiplas tentativas e delay configurável.
- **`HttpClient` dedicado para webhooks**: Cliente HTTP separado para envio de notificações, evitando conflitos de headers com o cliente da API Slack.

### Alterado
- **`Program.cs` completamente refatorado**: Código procedural reorganizado em 13 métodos agrupados em 7 regiões:
  - `Configuration and Setup`
  - `File Management`
  - `Data Loading and Saving`
  - `Slack API Integration`
  - `Data Processing`
  - `Slack Notifications`
  - `Utilities`
- **Credenciais removidas do código**: `SlackToken`, `SlackCookie`, `SlackTeamId` e URLs de webhook migrados de constantes hardcoded para User Secrets via `IConfiguration`.
- **`ManageUserInfoFiles`**: Retorna tupla `(string lastFile, string newFile, bool hasHistory)` para sinalizar existência de histórico.
- **`FindLastUserInfoFile`**: Retorna tupla `(string filePath, bool hasHistory)` indicando se o arquivo já existia ou foi criado na primeira execução.
- **Alinhamento de parâmetros**: Todos os métodos com múltiplos parâmetros padronizados para melhor legibilidade.
- **Mensagens do console**: Padronizadas com emojis e formato consistente.
- **Notificação condicional**: Envio ao Slack ocorre apenas quando `hasHistory == true`.

### Segurança
- Eliminado todo hardcoding de tokens, cookies e URLs de webhook.
- Credenciais armazenadas exclusivamente via .NET User Secrets (fora do repositório).
- `appsettings.json` mantém apenas estrutura sem valores sensíveis.

---

## [1.0.0] - 2024-12-06

### Adicionado
- Versão inicial do Bot Fofoqueiro.
- Integração com API `enterprise.teams.info` para contagem oficial de membros.
- Integração com API `users.list` para lista detalhada de usuários.
- Persistência de snapshots diários em arquivos JSON (`Desktop/SlackUserList/lastUserInfo_dd_MM_yyyy.txt`).
- Comparação entre snapshot anterior e estado atual para detectar entradas e saídas.
- Envio de relatório formatado via Slack Block Kit para dois canais via webhook.
- Filtragem de bots da lista de usuários.
- Busca retroativa de até 365 dias por arquivo de histórico existente.
