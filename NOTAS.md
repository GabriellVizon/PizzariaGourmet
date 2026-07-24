# Notas do Projeto - Dom Pizzaria

## O que foi feito até agora

### Limpeza e Remoções
- Deletados: bin/, obj/, app.db, .vscode/, uploads/, Checkout.cshtml (duplicado), start-pizzaria.ps1, fly.toml, Dockerfile, products.json, settings.json
- Páginas removidas: Historico, Rastreio, Admin/DeliveryPersons, Admin/DeliveryAreas, Admin/Customers
- APIs removidas: 15 rotas CRUD (delivery-persons, delivery-areas, customers)
- Código morto removido: usings desnecessários do Program.cs

### Renomeação → Dom Pizzaria
- Namespace `PizzariaGourmet` → `DomPizzaria` em ~30 arquivos
- Nome visual: "Dom Pizzaria"
- Email: `@dompizzaria.com`

### Melhorias de UX
- **Admin Navigation**: Todas as páginas admin mostram apenas "← Voltar para o Dashboard"
- **Order.cshtml**: Removida seção "Atribuir Entregador", adicionado botão voltar ao dashboard
- **Products**: Seletor de categoria movido antes do editor de tamanhos
- **admin-products.js**: Tamanhos variam por categoria (Pizza: P/M/G com diâmetro; Bebida: 300ml/500ml/1L)

### Datas
- Formatação brasileira em todas as páginas: `dd/MM/yyyy 'às' HH:mm` ou `dd/MM HH:mm`
- Layout já tem `lang="pt-BR"` para calendários do navegador em português

### Relatórios de Vendas (Reports.cshtml)
- Filtros rápidos: Hoje, 7 dias, 30 dias, Mês, Tudo
- Cards com fundo branco, valor em preto (#1a1a2e), label em cinza forte (#555)
- Tabelas estilizadas, CSV export melhorado
- ReportService: Adicionado `TotalDeliveryFees` ao record

### Links corrigidos
- `/Rastreio` → `/Admin/Order?id=` em NotificationService e WhatsAppService
- `/Historico` removido do Success.cshtml

### Acentos corrigidos
- Sobre.cshtml: História, paixão, até, experiência, rápida, dúvida, segurança, Horário, Terça a Sábado, às, configurações
- Contato.cshtml: rápido, Endereço, Paraíba, Horário, Terça a Sábado, às
- Layout: "Delivery em Paraíba do Sul" (acento mantido)

---

## O que falta fazer (análise completa disponível)

### CRÍTICAS
- WhatsApp hardcoded em 6 lugares (Layout, Success, Contato, Checkout) - deve usar configuração dinâmica
- PIX key hardcoded no Checkout.cshtml

### MÉDIAS
- Datas armazenadas como string (ISO) nos models Order, Customer, DeliveryPerson - deveria ser DateTime
- Formatos de data inconsistentes entre páginas (5+ formatos)
- Código morto: métodos de DeliveryService e CustomerService nunca chamados
- Performance: OrderService e ReportService carregam TODOS os pedidos na memória
- Funções JS duplicadas: formatPrice(), getItemTotal(), getBaseSubtotal() definidas 3x
- Labels sem `for` em todos os forms admin (acessibilidade)
- Status inconsistente: "Saiu pra entrega" vs "Saiu para entrega"
- Pagamento inconsistente: "Cartão" vs "Cartão de Crédito"

### MENORES
- Admin usa inline styles vs CSS classes (inconsistente)
- Credenciais admin hardcoded no appsettings.json
- Sem proteção CSRF no checkout
- `dayOfWeek: id` no Hours.cshtml é frágil

---

## Stack técnica
- ASP.NET Core 10 Razor Pages
- SQLite (planejado migração para Supabase/PostgreSQL)
- Identity para autenticação admin
- 9 serviços: OrderService, CouponService, ComplementService, ProductService, SettingsService, BusinessHoursService, DeliveryService, NotificationService, WhatsAppService, ReportService
- JS vanilla (site.js, admin-products.js, admin-coupons.js)
