# DomPizzaria

Sistema completo de delivery de pizzaria com cardápio online, carrinho, checkout, pagamento PIX/Dinheiro/Cartão, rastreio de pedidos e painel administrativo completo.

## Requisitos

- .NET 10 SDK
- SQLite (embutido, sem instalação necessária)

## Como rodar

```powershell
dotnet run
```

Acesse em http://localhost:5000

**Admin padrão:** `admin@pizzariagourmet.com` / `Admin@123`

## Funcionalidades

### Cliente
- Cardápio com categorias (Clássica, Especial, Premium, Bebida), busca por nome, tamanhos P/M/G, complementos
- Carrinho sidebar flutuante com quantidades e remoção
- Checkout com nome, telefone, e-mail, endereço, CPF, observações
- Pagamento: Cartão de Crédito, PIX (QR Code), Dinheiro (cálculo de troco)
- Cupons de desconto (percentual ou valor fixo)
- Rastreio de pedido por telefone ou número
- Histórico de pedidos por telefone

### Administrativo (`/Admin/Login`)
- Dashboard com pedidos pendentes, faturamento do dia, notificação sonora de novos pedidos
- Pedidos com filtros (nome, telefone, status, data), detalhes, atualização de status, impressão
- Confirmação manual de pagamento PIX/Dinheiro
- Produtos — CRUD com upload de imagens e editor de tamanhos
- Complementos — CRUD de ingredientes extras
- Cupons — CRUD com código, tipo, valor, pedido mínimo, validade, usos máximos
- Cozinha — Fila de pedidos para impressão
- Clientes — Lista com busca, histórico de pedidos, notas
- Áreas de entrega — CRUD com faixa de CEP, taxa, tempo estimado
- Entregadores — CRUD com disponibilidade
- Horário de funcionamento — Configuração por dia da semana
- Relatórios — Vendas por período
- Configurações — Taxa de entrega, frete grátis, WhatsApp, chave PIX, nome da loja

### Notificações
- E-mail para o cliente — Confirmação do pedido com resumo (HTML)
- E-mail para o admin — Aviso de novo pedido
- WhatsApp — Notificação de mudança de status (API HTTP configurável)
- Alerta sonoro no admin para novos pedidos

## Variáveis de Ambiente

### Opcionais — E-mail (recomendado)

| Variável | Descrição |
|---|---|
| `SMTP_HOST` | Servidor SMTP |
| `SMTP_PORT` | Porta SMTP (padrão: 587) |
| `SMTP_USER` | Usuário SMTP |
| `SMTP_PASS` | Senha SMTP |
| `NOTIFY_EMAIL_TO` | E-mail do admin para notificações |

### Opcionais — WhatsApp

| Variável | Descrição |
|---|---|
| `WHATSAPP_API_URL` | URL da API de WhatsApp |
| `WHATSAPP_API_KEY` | Chave da API |

### Opcionais — Geral

| Variável | Descrição |
|---|---|
| `DOMAIN` | URL pública do site (padrão: `http://localhost:5000`) |

### Admin (via `appsettings.json` ou env vars)

```powershell
$env:Admin__Email = "admin@seudominio.com"
$env:Admin__Password = "senha-forte-aqui"
```

## Para produção

1. Publicar: `dotnet publish -c Release -o ./publish`
2. Configurar variáveis de ambiente no servidor (SMTP, DOMAIN)
3. Configurar SMTP para envio de e-mails
4. Opcional: Configurar WhatsApp API para notificações

## Páginas

| Rota | Descrição |
|---|---|
| `/` | Cardápio com busca e categorias |
| `/Carrinho` | Revisão do carrinho |
| `/Checkout` | Finalizar pedido |
| `/Success` | Confirmação com número do pedido |
| `/Rastreio` | Rastrear pedido |
| `/Historico` | Histórico de pedidos |
| `/Admin/Login` | Login administrativo |
| `/Admin/Index` | Dashboard |
| `/Admin/Orders` | Gerenciar pedidos |
| `/Admin/Order` | Detalhe do pedido |
| `/Admin/Products` | Gerenciar produtos |
| `/Admin/Complements` | Gerenciar complementos |
| `/Admin/Coupons` | Gerenciar cupons |
| `/Admin/Kitchen` | Fila de impressão |
| `/Admin/Customers` | Clientes |
| `/Admin/DeliveryAreas` | Áreas de entrega |
| `/Admin/DeliveryPersons` | Entregadores |
| `/Admin/Hours` | Horário de funcionamento |
| `/Admin/Reports` | Relatórios |
| `/Admin/Settings` | Configurações |

## APIs

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
| GET | `/api/coupons/validate` | Validar cupom | Não |
| POST | `/api/coupons` | Criar cupom | Admin |
| PUT | `/api/coupons/{id}` | Atualizar cupom | Admin |
| DELETE | `/api/coupons/{id}` | Excluir cupom | Admin |
| POST | `/create-checkout-session` | Criar pedido | Não |
| GET | `/api/orders/new-count` | Contar novos pedidos | Não |
| POST | `/api/upload` | Upload de imagem | Admin |
| GET | `/api/business-hours` | Horários de funcionamento | Não |
| PUT | `/api/business-hours` | Atualizar horários | Admin |
| GET | `/api/business-hours/check` | Verificar se está aberto | Não |
| GET | `/api/delivery-areas` | Listar áreas de entrega | Não |
| POST | `/api/delivery-areas` | Criar área | Admin |
| PUT | `/api/delivery-areas/{id}` | Atualizar área | Admin |
| DELETE | `/api/delivery-areas/{id}` | Excluir área | Admin |
| GET | `/api/delivery-areas/check-cep` | Verificar CEP | Não |
| GET | `/api/delivery-persons` | Listar entregadores | Não |
| GET | `/api/delivery-persons/available` | Entregadores disponíveis | Não |
| POST | `/api/delivery-persons` | Criar entregador | Admin |
| PUT | `/api/delivery-persons/{id}` | Atualizar entregador | Admin |
| DELETE | `/api/delivery-persons/{id}` | Excluir entregador | Admin |
| GET | `/api/customers` | Listar clientes | Não |
| GET | `/api/customers/search` | Buscar clientes | Não |
| GET | `/api/customers/{id}` | Buscar cliente | Não |
| GET | `/api/customers/{id}/orders` | Pedidos do cliente | Não |
| PUT | `/api/customers/{id}/notes` | Atualizar notas | Admin |
| GET | `/api/reports` | Relatório de vendas | Admin |
| GET | `/api/kitchen/orders` | Pedidos para cozinha | Admin |
| POST | `/api/kitchen/print/{orderId}` | Marcar como impresso | Admin |
| POST | `/api/orders/{orderId}/assign` | Atribuir entregador | Admin |

## Tecnologias

- **.NET 10** — ASP.NET Core Razor Pages
- **SQLite** — Banco de dados local
- **Entity Framework Core** — ORM
- **ASP.NET Core Identity** — Autenticação admin
- **MailKit** — Envio de e-mails (SMTP)

## Estrutura do Projeto

```
DomPizzaria/
├── Data/                    # Banco de dados
│   └── app.db              # SQLite (criado automaticamente)
├── Models/                  # Entidades
│   ├── Product.cs
│   ├── Order.cs
│   ├── Complement.cs
│   ├── Coupon.cs
│   ├── Customer.cs
│   ├── DeliveryArea.cs
│   ├── DeliveryPerson.cs
│   └── BusinessHours.cs
├── Pages/                   # Razor Pages
│   ├── Admin/               # Painel administrativo (15 páginas)
│   ├── Carrinho.cshtml
│   ├── Checkout.cshtml
│   ├── Historico.cshtml
│   ├── Index.cshtml
│   ├── Rastreio.cshtml
│   └── Success.cshtml
├── Services/                # Lógica de negócio (9 serviços)
│   ├── ProductService.cs
│   ├── OrderService.cs
│   ├── ComplementService.cs
│   ├── CouponService.cs
│   ├── NotificationService.cs
│   ├── WhatsAppService.cs
│   ├── CustomerService.cs
│   ├── DeliveryService.cs
│   └── ReportService.cs
├── wwwroot/                 # Arquivos estáticos
├── Program.cs               # Entrypoint + rotas API
└── appsettings.json         # Configurações
```

## Segurança

- CSRF — Antiforgery tokens com header `X-CSRF-TOKEN`
- Headers HTTP — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`
- HSTS — Ativado em produção
- Autenticação — ASP.NET Identity com lockout (5 tentativas, 15 min)
- Cookies — HttpOnly + SameSite Strict
- Upload — Limitado a 5MB, tipos: jpg, jpeg, png, gif, webp
