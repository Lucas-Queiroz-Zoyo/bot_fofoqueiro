# Bot Fofoqueiro - Configuração de Segurança

Este projeto usa **User Secrets** do .NET para proteger informações sensíveis como tokens e webhooks do Slack.

## 🔐 Configuração Inicial

### 1. Configurar User Secrets

```bash
# Navegar para o diretório do projeto
cd bot_fofoqueiro

# Definir suas chaves reais (substitua pelos valores corretos)
dotnet user-secrets set "SlackBotConfiguration:SlackToken" "xoxc-SEU_TOKEN_REAL_AQUI"
dotnet user-secrets set "SlackBotConfiguration:SlackCookie" "SEU_COOKIE_COMPLETO_AQUI"
dotnet user-secrets set "SlackBotConfiguration:Webhooks:ChoroChannel" "https://hooks.slack.com/services/SEU/WEBHOOK/CHORO"
dotnet user-secrets set "SlackBotConfiguration:Webhooks:PrivateChannel" "https://hooks.slack.com/services/SEU/WEBHOOK/PRIVADO"
```

### 2. Verificar configuração

```bash
# Listar secrets configurados (sem mostrar os valores)
dotnet user-secrets list
```

## 📝 Onde encontrar as informações

- **SlackToken**: Token OAuth do Slack (começa com `xoxc-`)
- **SlackCookie**: Cookie completo de autenticação do navegador
- **Webhooks**: URLs dos webhooks criados no Slack

## 🚀 Executar o projeto

```bash
# Restaurar pacotes
dotnet restore

# Executar
dotnet run
```

## 🔒 Segurança

- ✅ **User Secrets**: Armazena dados sensíveis localmente fora do código
- ✅ **Gitignore**: Evita versionamento de arquivos de configuração
- ✅ **Template**: `appsettings.template.json` mostra a estrutura sem valores

## 📁 Arquivos importantes

- `appsettings.template.json` - Template de configuração (versionar)
- `appsettings.json` - Configuração base (versionar sem valores sensíveis)
- `SlackBotConfiguration.cs` - Modelo de configuração
- `.gitignore` - Exclusões de arquivos sensíveis

## ⚠️ NUNCA versionar

- Tokens reais do Slack
- Cookies de autenticação
- URLs completas de webhooks
- Arquivos `appsettings.Development.json` com dados reais