# PizzariaGourmet

Sistema completo de delivery de pizzaria com cardápio online, carrinho, checkout, pagamento, rastreio e painel administrativo.

## Requisitos

- .NET 10 SDK
- SQLite (embutido, sem instalação necessária)

## Como rodar

```powershell
cd PizzariaGourmet
dotnet run
```

Acesse em http://localhost:5000

## Funcionalidades

### Cliente
- **Cardápio** — Produtos com categorias, tamanhos (P/M/G), complementos
- **Carrinho** — Sidebar com quantidades, remove itens
- **Checkout** — Formulário com nome, telefone, endereço, CPF, observações
- **Pagamentos:**
  - **Cartão de Crédito** — Via Stripe (ambiente seguro)
  - **PIX** — QR Code dinâmico gerado no frontend com CRC16-CCITT
  - **Dinheiro** — Cálculo de troco automático
- **Cupons de desconto** — Percentual ou valor fixo, aplicados no checkout
- **Rastreio** — Acompanhar pedido por telefone ou número do pedido
- **Histórico** — Buscar todos os pedidos pelo telefone

### Administrativo (`/Admin/Login`)
- **Dashboard** — Resumo com pedidos pendentes, hoje, faturamento, notificação sonora de novos pedidos
- **Pedidos** — Lista com filtros (nome, telefone, status, data), detalhes, atualização de status, impressão, exclusão
- **Produtos** — CRUD completo com upload de imagens e editor de tamanhos
- **Complementos** — CRUD de ingredientes extras
- **Cupons** — CRUD com código, tipo, valor, pedido mínimo, validade, usos máximos
- **Configurações** — Taxa de entrega, frete grátis, WhatsApp da loja, chave PIX, nome da loja

### Notificações
- **WhatsApp** — Notificação ao cliente sobre mudança de status (API HTTP configurável)
- **Email** — Via SMTP (opcional)
- **SMS** — Via Twilio (opcional)
- **Som** — Alerta sonoro no admin para novos pedidos

## Variáveis de Ambiente

### Stripe (pagamento com cartão)
```powershell
$env:STRIPE_API_KEY = "sk_test_..."
$env:STRIPE_WEBHOOK_SECRET = "whsec_..."
$env:DOMAIN = "http://localhost:5000"
```

### WhatsApp (notificações para o cliente)
```powershell
$env:WHATSAPP_API_URL = "https://seu-provedor/api/enviar"
$env:WHATSAPP_API_KEY = "sua-chave"
```
Compatível com Evolution API, Z-API, WhatsApp Cloud API, etc.
Se não configurado, as notificações são ignoradas silenciosamente.

### Email (notificações para o admin)
```powershell
$env:SMTP_HOST = "smtp.exemplo.com"
$env:SMTP_PORT = "587"
$env:SMTP_USER = "seu@email.com"
$env:SMTP_PASS = "sua-senha"
$env:NOTIFY_EMAIL_TO = "admin@exemplo.com"
```

### SMS (Twilio)
```powershell
$env:TWILIO_ACCOUNT_SID = "ACxxx"
$env:TWILIO_AUTH_TOKEN = "xxx"
$env:TWILIO_FROM = "+15551234567"
$env:NOTIFY_PHONE_TO = "+5511999999999"
```

### Admin (credenciais padrão — configurável via `appsettings.json`)
```
Email: admin@pizzariagourmet.com
Senha: Admin@123
```

## Páginas

| Rota | Descrição |
|---|---|
| `/` | Cardápio |
| `/Carrinho` | Revisão do carrinho |
| `/Checkout` | Finalizar pedido |
| `/Success` | Confirmação do pedido |
| `/Rastreio` | Rastrear pedido |
| `/Historico` | Histórico de pedidos |
| `/Admin/Login` | Login administrativo |
| `/Admin/Index` | Dashboard admin |
| `/Admin/Orders` | Gerenciar pedidos |
| `/Admin/Order` | Detalhe do pedido |
| `/Admin/Products` | Gerenciar produtos |
| `/Admin/Complements` | Gerenciar complementos |
| `/Admin/Coupons` | Gerenciar cupons |
| `/Admin/Settings` | Configurações da loja |

## APIs

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| GET | `/api/settings` | Configurações da loja | Não |
| PUT | `/api/settings` | Atualizar configurações | Admin |
| GET | `/api/products` | Listar produtos | Não |
| POST | `/api/products` | Criar produto | Admin |
| PUT | `/api/products/{id}` | Atualizar produto | Admin |
| DELETE | `/api/products/{id}` | Excluir produto | Admin |
| GET | `/api/complements` | Listar complementos | Não |
| GET | `/api/complements/available` | Complementos disponíveis | Não |
| POST | `/api/complements` | Criar complemento | Admin |
| PUT | `/api/complements/{id}` | Atualizar complemento | Admin |
| DELETE | `/api/complements/{id}` | Excluir complemento | Admin |
| GET | `/api/coupons` | Listar cupons | Não |
| GET | `/api/coupons/validate` | Validar cupom | Não |
| POST | `/api/coupons` | Criar cupom | Admin |
| PUT | `/api/coupons/{id}` | Atualizar cupom | Admin |
| DELETE | `/api/coupons/{id}` | Excluir cupom | Admin |
| POST | `/create-checkout-session` | Criar pedido | Não |
| GET | `/api/orders/new-count` | Novos pedidos (polling) | Não |
| POST | `/api/upload` | Upload de imagem | Admin |
| POST | `/webhook` | Webhook Stripe | Não |
| GET | `/session/{id}` | Consultar sessão Stripe | Não |
