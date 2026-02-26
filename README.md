# Ecommerce Laptop

Fullstack e-commerce platform for laptops. Built as an intern project to learn Clean Architecture, payment integration, and AI-assisted features.

**Backend:** .NET 9  EF Core 9  SQL Server  Redis  Typesense  Qdrant  
**Frontend:** Next.js 16  React 19  TypeScript  Tailwind CSS 4  
**Infra:** Docker Compose  Prometheus  Grafana  Loki

---

## Screenshots

| Product Listing | Product Details |
|:---:|:---:|
| ![Product Listing](docs/screenshots/List.png) | ![Product Details](docs/screenshots/Product.png) |

| AI Chatbot | Admin  LLM Config |
|:---:|:---:|
| ![AI Chatbot](docs/screenshots/ChatBot.png) | ![LLM Config](docs/screenshots/LLMConfig.png) |

---

## Table of Contents

- [What This Project Does](#what-this-project-does)
- [Tech Stack](#tech-stack)
- [Notable Implementations](#notable-implementations)
- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Testing](#testing)
- [Known Limitations](#known-limitations)
- [Documentation](#documentation)

---

## What This Project Does

An e-commerce site where users can browse laptops, search with typo tolerance, pay through 3 gateways, and chat with an AI assistant that knows the product catalog.

The admin side covers product/order/customer management, blog CMS, security monitoring, PDF/Excel exports, and LLM configuration.

Backend follows a **3-layer structure**

---

## Tech Stack

### Backend

| Technology | Purpose |
|---|---|
| .NET 9 / ASP.NET Core | Web API |
| Entity Framework Core 9 | ORM, migrations |
| SQL Server | Primary database |
| Redis 7 | Cache, session store |
| Typesense | Full-text search (typo tolerance, facets) |
| Qdrant | Vector DB for RAG embeddings |
| Microsoft Semantic Kernel | AI orchestration (chat, embeddings, tool calling) |
| SignalR | Real-time streaming (chatbot, notifications) |
| Hangfire | Background jobs |
| Serilog + Loki | Structured logging, centralized log aggregation |
| OpenTelemetry + Prometheus | Metrics collection |
| iText7 / ClosedXML | PDF and Excel export |
| Stripe, PayPal, SePay | Payment gateways |
| MailKit | Transactional emails |
| BCrypt.Net | Password hashing |

### Frontend

| Technology | Purpose |
|---|---|
| Next.js 16 (App Router) | Framework, mostly client-rendered |
| React 19, TypeScript 5 | UI, type safety |
| Tailwind CSS 4, Radix UI | Styling, accessible primitives |
| Zustand | Client state management |
| TanStack Query / Table | Server state, data tables |
| React Hook Form + Zod | Forms, validation |
| next-auth | Authentication |
| Framer Motion | Animations |
| Recharts | Admin dashboard charts |

### Infrastructure

| Technology | Purpose |
|---|---|
| Docker Compose | Multi-container dev environment |
| Prometheus + Grafana | Metrics dashboards |
| Loki + Promtail | Log aggregation |
| Nginx | Reverse proxy (production) |
| xUnit, Jest, Playwright | Testing |

---

## Notable Implementations

### AI Chatbot (RAG)

The chatbot uses Retrieval-Augmented Generation so it can answer questions about actual products in the database rather than hallucinating.

- Product data is chunked and embedded into Qdrant using Semantic Kernel
- User messages go through intent classification (product search, order tracking, general)
- Relevant product chunks are retrieved from Qdrant and injected into the LLM prompt
- Responses stream token-by-token over SignalR
- Hash-based query cache to avoid repeated LLM calls on identical questions
- The LLM can call backend tools (search products, look up orders) via tool calling
- Simple keyword-based guardrail on LLM input/output
- Admins can change the model, provider, temperature, and system prompt from the dashboard

### Payment Gateways

Three gateways implemented using Strategy + Factory pattern (`PaymentServiceFactory`, `PaymentOrchestrator`):

| Gateway | Type | Market |
|---|---|---|
| Stripe | Card only | International |
| PayPal | PayPal Checkout | International |
| SePay | Bank transfer (QR code) | Vietnam |

Webhook signature verification and payment audit logging on all gateways. Idempotency service written but not yet wired up.

### Search (Typesense)

- Typo-tolerant full-text search
- Faceted filtering (brand, category, price, specs)
- Autocomplete suggestions
- Auto re-indexing on product changes via domain events

### Security Features

- JWT with access token (15 min) + refresh token rotation (7 days)
- Separate customer/admin token flows with claims-based RBAC
- IP whitelist/blacklist middleware
- Rate limiting (per minute/hour)
- Field-level encryption for sensitive DB fields (DataProtection)
- Security event tracking for failed logins and IP blocks
- Audit logging for admin actions
- BCrypt password hashing with complexity rules

### Monitoring & Logging

- Prometheus scrapes HTTP and custom AI metrics  Grafana dashboards
- Serilog writes structured JSON logs  Loki  Grafana Log Explorer
- Promtail collects system-level logs
- OpenTelemetry metrics for ASP.NET Core

---

## Features

### Customer Side

- **Search & browse**  full-text search, filters (brand/category/price/specs), sorting, pagination
- **Product details**  image gallery with zoom, specs, variants (CPU/RAM/storage), reviews, related products
- **Product comparison**  side-by-side spec table
- **Cart**  guest + logged-in, auto-merge on login, stock validation, discount codes, bundles
- **Checkout**  multi-step flow, 3 payment methods, retry mechanism
- **User account**  profile, order history, addresses, reviews, wishlist, VIP tier
- **Auth**  registration with email confirmation, password reset, brute-force protection
- **AI Chatbot**  floating chat with streaming responses, product/order carousels, quick action chips, history persistence
- **Blog**  posts with MDX rendering, categories, tags
- **Static pages**  about, contact, store locator, support, promotions, legal

### Admin Panel

- **Dashboard**  revenue/order/customer stats, charts, top products
- **Products**  CRUD, image upload (ImgBB), variants, bundles, categories, brands, import/export
- **Orders**  list, detail, status workflow (Pending  Confirmed  Shipped  Delivered  Completed), refunds, audit trail
- **Customers**  search, detail view, purchase history
- **Blog**  CRUD with rich text editor, categories, tags
- **Security**  event log, IP blocking, rate limit stats, alerts
- **Reports**  PDF invoices/inventory (iText7), Excel exports (ClosedXML)
- **Settings**  system config, currency, LLM config, user/role/permission management (RBAC)

---

## Architecture

Dependency direction: both API and Infrastructure depend on Core. Core has no outward project references.

```
                    
                            Core           
                      entities, interfaces 
                      DTOs, specs, events  
                    
                                     
                                     
              depends on               depends on
                                     
              
               API Layer        Infrastructure     
              controllers       services, repos    
              hubs, DI          EF Core, middleware
              
                                         
                     API also references Infrastructure
                          
                           
```

### External Services

```
            
              Next.js 16  
              frontend    
            
                    HTTP / SignalR
                   
            
              .NET API     SQL Server
                           Redis (cache)
                           Qdrant (vector DB)
                           Typesense (search)
                           Stripe / PayPal / SePay
                           LLM provider (OpenAI, etc.)
            
```

### Monitoring

```
API metrics Prometheus  Grafana (dashboards)
 
 logs Serilog  Loki  Grafana (log explorer)
                         
                     Promtail (system logs)
```

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [SQL Server](https://www.microsoft.com/sql-server) (or LocalDB)

### 1. Clone

```bash
git clone https://github.com/QuocQQuang/EcommerceLaptop.git
cd EcommerceLaptop
```

### 2. Start infrastructure

```bash
docker-compose up -d
```

This starts Redis, Qdrant, Typesense, Prometheus, Grafana, Loki, and Promtail.

### 3. Configure and run backend

```bash
cd src/EcommerceLaptop.API
cp appsettings.example.json appsettings.json
# Edit appsettings.json  fill in connection strings and API keys
dotnet run
```

API available at `https://localhost:5129`.

### 4. Configure and run frontend

```bash
cd ecommerce-laptop-frontend
cp .env.example .env.local
# Edit .env.local  set API URL
npm install ; npm run dev
```

Frontend available at `http://localhost:3000`.

### Quick start (Windows)

```bash
.\start-quick.ps1
# or
.\start-quick.bat
```

### Demo admin account

- URL: `http://localhost:3000/admin-login`
- Email: `superadmin@ecommerce.com`
- Password: `Admin123!`

---

## Project Structure

```
EcommerceLaptop/
 src/
    EcommerceLaptop.API/        # API layer
       Controllers/                      # 34 controllers (incl. Admin/)
       Authorization/                    # JWT policy definitions
       Hubs/                             # SignalR (ChatHub)
       Seed/                             # Data seeders
       EmailTemplates/                   # HTML email templates
       Program.cs                        # Entry point, DI config
   
    EcommerceLaptop.Core/       # Core layer (no external deps)
       Entities/                         # 21 domain entities
       Interfaces/                       # 27 interfaces
       DTOs/                             # Data transfer objects
       Specifications/                   # Specification pattern
       DomainEvents/                     # Domain events
       ValueObjects/
   
    EcommerceLaptop.Infrastructure/
       Services/                         # ~80 service implementations
          AI/                           # RAG, embeddings, vector, chat
          Payment/                      # Stripe, PayPal, SePay
          Security/                     # IP blocking, rate limit, audit
       Repositories/
       Data/                             # EF Core DbContext
       Middleware/
       Migrations/
   
    BlogDataSeeder/                       # Blog seed data tool
    ReviewDataSeeder/                     # Review seed data tool

 ecommerce-laptop-frontend/                # Next.js 16
    src/
        app/                              # App Router pages
        components/                       # ~130 React components
        services/                         # 20 API service modules
        store/                            # Zustand stores
        hooks/                            # Custom hooks
        types/                            # TypeScript types

 tests/
    EcommerceLaptop.UnitTests/  # 10 test files
    EcommerceLaptop.IntegrationTests/  # 6 test files

 docker-compose.yml
 prometheus.yml
 monitoring/
```

---

## Testing

The project has basic test coverage:

- **Unit tests** (xUnit)  10 files covering inventory, product, order, cart, stock management, and intent classification
- **Integration tests** (xUnit)  6 files covering auth, cart, orders, products, users, and chat persistence
- **Frontend tests**  Jest for unit tests, Playwright for E2E

```bash
# Run backend tests
dotnet test

# Run frontend tests
cd ecommerce-laptop-frontend
npm test
npm run test:e2e
```

---

## Known Limitations

- Most services untested
- No distributed tracing, only metrics via OpenTelemetry
- Security features not pen-tested
- No social login
- VIP tier: read/assign works, auto-upgrade stubbed
- Brute force: per-user lockout works, cross-account detection returns false
- Inconsistent error handling across endpoints

---

## Documentation

| Document | Description |
|---|---|
| [Backend Features](docs/backend-features.md) | Detailed breakdown of backend modules |
| [Swagger](https://localhost:5129/swagger) | API docs (when running locally) |
| [Frontend README](ecommerce-laptop-frontend/README.md) | Frontend setup and guide |
| `appsettings.example.json` | Backend config template |
| `.env.example` | Frontend config template |

---

*Intern project  .NET 9 & Next.js 16*
