# PizzariaGourmet 🍕

Sistema completo de delivery de pizzaria com cardápio online, carrinho, checkout, pagamento Stripe/PIX/Dinheiro, rastreio de pedidos e painel administrativo completo.

> **Produto pronto para implantar.** Basta configurar as variáveis de ambiente e colocar no ar.

---

## Requisitos

- .NET 10 SDK
- SQLite (embutido, sem instalação necessária)
- Conta [Stripe](https://stripe.com) (para pagamento com cartão) — **opcional**, PIX e Dinheiro funcionam sem
- (Opcional) Conta SMTP (SendGrid, Mailgun, etc.) para e-mails de confirmação

## Como rodar (desenvolvimento)

```powershell
cd PizzariaGourmet
$env:STRIPE_API_KEY = "sk_test_..."   # opcional, apenas para cartão
dotnet run
```

Acesse em http://localhost:5000

**Admin padrão:** `admin@pizzariagourmet.com` / `Admin@123`

---

## Funcionalidades

### 👤 Cliente
- **Cardápio** — Produtos com categorias (Clássica, Especial, Premium, Bebida), busca por nome, tamanhos P/M/G, complementos
- **Carrinho** — Sidebar flutuante com quantidades, remoção de itens
- **Checkout** — Formulário com nome, telefone, e-mail, endereço, CPF, observações
- **Pagamentos:**
  - **Cartão de Crédito** — Via Stripe Checkout (ambiente seguro)
  - **PIX** — QR Code gerado no frontend com CRC16-CCITT
  - **Dinheiro** — Cálculo de troco automático
- **Cupons de desconto** — Percentual ou valor fixo
- **Rastreio** — Acompanhar pedido por telefone ou número do pedido (com timeline visual)
- **Histórico** — Todos os pedidos de um telefone

### 🔧 Administrativo (`/Admin/Login`)
- **Dashboard** — Resumo com pedidos pendentes, hoje, faturamento, + notificação sonora de novos pedidos (polling a cada 30s)
- **Pedidos** — Lista com filtros (nome, telefone, status, data), detalhes, atualização de status, impressão, exclusão
- **Confirmação manual PIX/Dinheiro** — Botão em destaque para marcar pagamento como recebido
- **Produtos** — CRUD completo com upload de imagens e editor de tamanhos (P/M/G/MR)
- **Complementos** — CRUD de ingredientes extras
- **Cupons** — CRUD com código, tipo (percentual/fixo), valor, pedido mínimo, validade, usos máximos
- **Configurações** — Taxa de entrega, frete grátis, WhatsApp da loja, chave PIX, nome da loja

### 📬 Notificações
- **E-mail para o cliente** — Confirmação do pedido com resumo e link de rastreio (HTML)
- **E-mail para o admin** — Aviso de novo pedido (via `NOTIFY_EMAIL_TO`)
- **WhatsApp** — Notificação ao cliente sobre mudança de status (API HTTP configurável: Evolution, Z-API, WhatsApp Cloud)
- **Alerta sonoro** — No admin para novos pedidos (via Web Audio API)
- **Fallback** — Todos os serviços têm fallback silencioso se não configurados

---

## 🔑 Variáveis de Ambiente

### Obrigatórias para cartão de crédito

| Variável | Descrição |
|---|---|
| `STRIPE_API_KEY` | Chave secreta do Stripe (começa com `sk_`) |
| `DOMAIN` | URL pública do site (ex: `https://seudominio.com`). Padrão: `http://localhost:5000` |

### Opcionais — e-mail (recomendado)

| Variável | Descrição |
|---|---|
| `SMTP_HOST` | Servidor SMTP (ex: `smtp.sendgrid.net`) |
| `SMTP_PORT` | Porta SMTP (padrão: `587`) |
| `SMTP_USER` | Usuário SMTP |
| `SMTP_PASS` | Senha SMTP |
| `NOTIFY_EMAIL_TO` | E-mail do admin para receber notificações de novos pedidos |

> Sem SMTP: cliente **não** recebe confirmação por e-mail, mas o pedido ainda é criado normalmente.

### Opcionais — WhatsApp

| Variável | Descrição |
|---|---|
| `WHATSAPP_API_URL` | URL da API de WhatsApp (Evolution, Z-API, etc.) |
| `WHATSAPP_API_KEY` | Chave da API |

### Opcionais — Stripe Webhook (produção)

| Variável | Descrição |
|---|---|
| `STRIPE_WEBHOOK_SECRET` | Secret do webhook Stripe (começa com `whsec_`) |

> Sem webhook: a confirmação do pagamento é feita via `POST /api/orders/confirm-payment` chamado pela página de sucesso. O webhook é o método **recomendado** em produção, mas não obrigatório.

### Opcionais — SMS

| Variável | Descrição |
|---|---|
| `TWILIO_ACCOUNT_SID` | Twilio Account SID |
| `TWILIO_AUTH_TOKEN` | Twilio Auth Token |
| `TWILIO_FROM` | Número de origem Twilio |
| `NOTIFY_PHONE_TO` | Telefone do admin para SMS |

### Admin (configurável via `appsettings.json` ou env vars)

Para configurar via variáveis de ambiente no lugar do `appsettings.json`:
```powershell
$env:Admin__Email = "admin@seudominio.com"
$env:Admin__Password = "senha-forte-aqui"
```

**Padrão (desenvolvimento):**
```
Email: admin@pizzariagourmet.com
Senha: Admin@123
```

---

## 🚀 Para colocar em produção

1. **Publicar o app:**
   ```powershell
   dotnet publish -c Release -o ./publish
   ```
2. **Configurar variáveis de ambiente** no servidor (Stripe, SMTP, DOMAIN)
3. **Configurar webhook no Stripe** (recomendado):
   - Criar endpoint: `https://seudominio.com/webhook`
   - Assinar evento: `checkout.session.completed`
   - Copiar o `Signing secret` para `STRIPE_WEBHOOK_SECRET`
4. **Configurar SMTP** para enviar e-mails de confirmação aos clientes
5. **Opcional:** Configurar WhatsApp API para notificações automáticas

---

## 📄 Páginas

| Rota | Descrição |
|---|---|
| `/` | Cardápio com busca e categorias |
| `/Carrinho` | Revisão do carrinho |
| `/Checkout` | Finalizar pedido (dados + pagamento) |
| `/Success` | Confirmação com número do pedido |
| `/Rastreio` | Rastrear pedido (telefone ou ID) |
| `/Historico` | Histórico de pedidos por telefone |
| `/Admin/Login` | Login administrativo |
| `/Admin/Index` | Dashboard admin |
| `/Admin/Orders` | Gerenciar pedidos |
| `/Admin/Order` | Detalhe do pedido |
| `/Admin/Products` | Gerenciar produtos |
| `/Admin/Complements` | Gerenciar complementos |
| `/Admin/Coupons` | Gerenciar cupons |
| `/Admin/Settings` | Configurações da loja |

---

## 🌐 APIs

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| GET | `/api/settings` | Configurações da loja | Não |
| PUT | `/api/settings` | Atualizar configurações | Admin |
| GET | `/api/products` | Listar produtos | Não |
| GET | `/api/products/{id}` | Buscar produto | Não |
| POST | `/api/products` | Criar produto | Admin |
| PUT | `/api/products/{id}` | Atualizar produto | Admin |
| DELETE | `/api/products/{id}` | Excluir produto | Admin |
| GET | `/api/complements` | Listar complementos | Não |
| GET | `/api/complements/available` | Complementos disponíveis | Não |
| GET | `/api/complements/{id}` | Buscar complemento | Não |
| POST | `/api/complements` | Criar complemento | Admin |
| PUT | `/api/complements/{id}` | Atualizar complemento | Admin |
| DELETE | `/api/complements/{id}` | Excluir complemento | Admin |
| GET | `/api/coupons` | Listar cupons | Não |
| GET | `/api/coupons/validate?code=XXX&subtotal=100` | Validar cupom | Não |
| POST | `/api/coupons` | Criar cupom | Admin |
| PUT | `/api/coupons/{id}` | Atualizar cupom | Admin |
| DELETE | `/api/coupons/{id}` | Excluir cupom | Admin |
| POST | `/create-checkout-session` | Criar pedido + sessão Stripe | Não |
| GET | `/api/orders/new-count?since=DATA` | Contar novos pedidos (polling) | Não |
| **POST** | **`/api/orders/confirm-payment`** | **Confirmar pagamento após Stripe redirect** (corpo: `{"session_id":"..."}`) | **Não** |
| POST | `/api/upload` | Upload de imagem (max 5MB, tipos: jpg/png/gif/webp) | Admin |
| POST | `/webhook` | Webhook Stripe | Não |
| GET | `/session/{id}` | Consultar sessão Stripe | Não |

---

## 🧱 Tecnologias

- **.NET 10** — ASP.NET Core Razor Pages
- **SQLite** — Banco de dados local (sem servidor)
- **Entity Framework Core** — ORM
- **ASP.NET Core Identity** — Autenticação admin
- **Stripe** — Pagamento com cartão de crédito
- **MailKit** — Envio de e-mails (SMTP)
- **Twilio** — SMS (opcional)

---

## 📁 Estrutura do Projeto

```
PizzariaGourmet/
├── Data/                 # Banco de dados + JSONs de seed
│   ├── app.db            # SQLite (criado automaticamente)
│   ├── products.json     # Produtos iniciais (11 itens)
│   └── settings.json     # Configurações editáveis via admin
├── Models/               # Entidades (Product, Order, Coupon, etc.)
├── Pages/                # Razor Pages
│   ├── Admin/            # Painel administrativo (protegido)
│   ├── Carrinho.cshtml
│   ├── Checkout.cshtml
│   ├── Historico.cshtml
│   ├── Index.cshtml      # Cardápio
│   ├── Rastreio.cshtml
│   └── Success.cshtml
├── Services/             # Lógica de negócio
│   ├── ProductService.cs
│   ├── OrderService.cs
│   ├── ComplementService.cs
│   ├── CouponService.cs
│   ├── NotificationService.cs  # Email + SMS
│   └── WhatsAppService.cs
├── wwwroot/              # Arquivos estáticos (CSS, JS, imagens)
├── Program.cs            # Entrypoint + todas as rotas API
└── appsettings.json      # Config (admin padrão)
```

---

## 🔒 Segurança

- **CSRF** — Antiforgery tokens habilitado com header `X-CSRF-TOKEN`
- **Headers HTTP** — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`
- **HSTS** — Ativado em produção (fora de localhost)
- **Autenticação** — ASP.NET Identity com lockout (5 tentativas, 15 min)
- **Cookies** — HttpOnly + SameSite Strict
- **Upload** — Limitado a 5MB, tipos permitidos: jpg, jpeg, png, gif, webp
