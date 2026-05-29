PizzariaGourmet — Protótipo ASP.NET Core (minimal)

Requisitos:
- .NET 10 SDK instalado (ou ajuste o TargetFramework se necessário)

Instalação de dependências:

```powershell
cd C:\Users\Etec\Desktop\PizzariaGourmet
dotnet restore
dotnet add package Stripe.net
```

Como rodar:

```powershell
cd C:\Users\Etec\Desktop\PizzariaGourmet
dotnet run
```

Páginas:
- `/` — Página do cardápio (consome `/api/products`)
- `/Checkout` — Formulário de checkout (envia para `/api/checkout`)

Pagamento:
- Este scaffold inclui integração com Stripe.

Configurar as variáveis de ambiente antes de rodar (Windows PowerShell exemplo):

```powershell
$env:STRIPE_API_KEY = "sk_test_..."
$env:STRIPE_WEBHOOK_SECRET = "whsec_..." # opcional para validar webhooks
$env:DOMAIN = "https://seu-dominio.com" # URL pública usada nos retornos
dotnet run
```

Fluxo do Stripe:
- Cliente chama `/create-checkout-session` com o `cart` no body (JSON). O servidor cria uma `Session` no Stripe e retorna `url` para redirecionar o usuário para o checkout seguro da Stripe.
- Webhook `/webhook` verifica eventos (ex.: `checkout.session.completed`) e deve marcar o pedido como pago no seu banco.

Observação: as chaves privadas devem ficar fora do código — use variáveis de ambiente ou um secret manager.

Testando webhooks localmente:

1. Instale o Stripe CLI: https://stripe.com/docs/stripe-cli
2. Rode o seu app localmente: `dotnet run`
3. No terminal, rode (exemplo):

```powershell
stripe login
stripe listen --forward-to localhost:5000/webhook
```

Isso vai encaminhar eventos do Stripe para seu endpoint `/webhook` local para testes.

Observações sobre custos: o Stripe não cobra mensalidade, mas aplica taxas por transação. Para um gateway totalmente gratuito sem taxas, geralmente não há opção confiável — marketplaces/bancos locais podem ter alternativas.

Configurar envio de email (notificações):

Defina as seguintes variáveis de ambiente para que o webhook envie um email ao confirmar o pagamento:

```
SMTP_HOST=smtp.exemplo.com
SMTP_PORT=587
SMTP_USER=seu_usuario
SMTP_PASS=sua_senha
NOTIFY_EMAIL_TO=seu@email.com
```

Você pode usar o SMTP do Gmail (requer app password) ou um serviço como SendGrid/Mailgun (tem planos gratuitos com limites). O envio de email é opcional — se as variáveis não estiverem configuradas, o webhook continuará atualizando o pedido no banco.

Configurar SMS/WhatsApp via Twilio (opcional):

```
TWILIO_ACCOUNT_SID=ACxxx
TWILIO_AUTH_TOKEN=xxxx
TWILIO_FROM=+1555...           # exemplo: "+15551234567" ou "whatsapp:+1415..."
NOTIFY_PHONE_TO=+55XXXXXXXXX   # destino (pode ser whatsapp:+55... para WhatsApp via Twilio)
```

Observação: o Twilio tem plano trial e cobra por mensagens/uso; para WhatsApp via Twilio é necessário número habilitado em sandbox/produção.

Personalização rápida:
- Substitua `Data/products.json` pelos produtos reais.
- Adicione imagens em `wwwroot/images/` e atualize os caminhos no JSON.

Se quiser, eu já configuro a integração com Stripe ou adapto para C# ASP.NET com um painel de administração — diga qual gateway prefere e eu implemento os trechos necessários e explico como funcionar.