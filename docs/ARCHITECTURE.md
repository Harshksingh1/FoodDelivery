# FoodDelivery — Full Architecture Documentation

> Reference document for HLD, LLD, and DB diagram creation.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Tech Stack](#2-tech-stack)
3. [Infrastructure & Ports](#3-infrastructure--ports)
4. [API Gateway](#4-api-gateway)
5. [AuthService](#5-authservice)
6. [CatalogService](#6-catalogservice)
7. [OrderService](#7-orderservice)
8. [PaymentService](#8-paymentservice)
9. [AdminService](#9-adminservice)
10. [Shared Library](#10-shared-library)
11. [RabbitMQ — Events & Saga](#11-rabbitmq--events--saga)
12. [Database Schemas](#12-database-schemas)
13. [Frontend — Angular](#13-frontend--angular)
14. [Cross-Cutting Concerns](#14-cross-cutting-concerns)
15. [User Roles & Permissions](#15-user-roles--permissions)
16. [Order Lifecycle Flow](#16-order-lifecycle-flow)

---

## 1. System Overview

FoodDelivery is a cloud-ready .NET 10 food delivery platform built with Clean Architecture microservices. All client traffic enters through an Ocelot API Gateway. Services communicate asynchronously via RabbitMQ (MassTransit). Each service owns its own SQL Server database (database-per-service pattern).

```
Angular SPA (port 4200)
        │
        ▼
Ocelot API Gateway (port 5000)
        │
   ┌────┴──────────────────────────────────────┐
   │                                           │
AuthService  CatalogService  OrderService  PaymentService  AdminService
 (5001)        (5002)          (5003)        (5004)          (5005)
   │              │               │              │
   └──────────────┴───────────────┴──────────────┘
                          │
                     RabbitMQ (5672)
                          │
              SQL Server (1433) — separate DB per service
```

---

## 2. Tech Stack

| Concern | Technology |
|---|---|
| Framework | .NET 10 / ASP.NET Core |
| API Gateway | Ocelot + MMLib.SwaggerForOcelot |
| Message Broker | RabbitMQ 3.13 via MassTransit |
| ORM | Entity Framework Core 10 |
| Database | SQL Server 2022 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Logging | Serilog (Console + rolling file) |
| Testing | NUnit |
| Containerisation | Docker / Docker Compose |
| Frontend | Angular 17, Tailwind CSS, Signals |

---

## 3. Infrastructure & Ports

| Service | Local Port | Docker Container | Database |
|---|---|---|---|
| API Gateway | 5000 | fd-gateway | — |
| AuthService | 5001 | fd-auth-api | AuthDb |
| CatalogService | 5002 | fd-catalog-api | CatalogDb |
| OrderService | 5003 | fd-order-api | OrderDb |
| PaymentService | 5004 | fd-payment-api | PaymentDb |
| AdminService | 5005 | fd-admin-api | — (no own DB) |
| SQL Server | 1433 | fd-sqlserver | — |
| RabbitMQ AMQP | 5672 | fd-rabbitmq | — |
| RabbitMQ UI | 15672 | fd-rabbitmq | — |
| Angular Dev | 4200 | — | — |

AdminService has no own database — it aggregates data by calling other services over HTTP.

---

## 4. API Gateway

**Project:** `Gateway/FoodDelivery.Gateway`
**Framework:** Ocelot + JWT validation

### Route Mappings (ocelot.json)

| Gateway Upstream | Downstream Service | Port |
|---|---|---|
| `/gateway/auth/{everything}` | `/api/auth/{everything}` | 5001 |
| `/gateway/catalog/{everything}` | `/api/catalog/{everything}` | 5002 |
| `/gateway/cart/{everything}` | `/api/cart/{everything}` | 5003 |
| `/gateway/orders/{everything}` | `/api/orders/{everything}` | 5003 |
| `/gateway/payments/{everything}` | `/api/payments/{everything}` | 5004 |
| `/gateway/admin/{everything}` | `/api/admin/{everything}` | 5005 |

The gateway also aggregates Swagger UIs from all services at `/swagger`.

---

## 5. AuthService

**Port:** 5001 | **Database:** AuthDb | **Project:** `Services/AuthService`

### Architecture Layers

```
AuthService.API          → Controllers, Program.cs
AuthService.Application  → IAuthAppService, AuthAppService, DTOs
AuthService.Domain       → User, RefreshToken, IUserRepository, IRefreshTokenRepository, UserRole enum
AuthService.Infrastructure → AuthDbContext, UserRepository, RefreshTokenRepository, JwtTokenService, EmailService
```

### Domain Entities

**User** (extends `IdentityUser<Guid>`)
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| FullName | string | max 200, required |
| Email | string | unique (Identity) |
| Mobile | string | max 20, unique index |
| Role | UserRole enum | stored as string |
| IsActive | bool | default true |
| ProfileImageUrl | string? | |
| OtpCode | string? | 6-digit, cleared after use |
| OtpExpiry | DateTime? | 10 min window |
| OtpSessionToken | string? | GUID, used to correlate OTP verify |
| PasswordResetToken | string? | GUID, 1 hr expiry |
| PasswordResetTokenExpiry | DateTime? | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | |

**RefreshToken**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Token | string | 64-byte random base64 |
| UserId | Guid | FK → User |
| ExpiresAt | DateTime | 30 days |
| IsRevoked | bool | default false |
| CreatedAt | DateTime | |

**UserRole enum:** `Customer`, `RestaurantPartner`, `Admin`, `DeliveryAgent`

**RegistrationRole enum:** `Customer`, `RestaurantPartner`, `DeliveryAgent` (Admin cannot self-register)

### API Endpoints — AuthController `[/api/auth]`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/register` | Public | Register new user (Customer/Partner/Agent) |
| POST | `/login` | Public | Step 1: validate credentials, send OTP (Admin skips OTP) |
| POST | `/login/verify-otp` | Public | Step 2: submit OTP, receive JWT + refresh token |
| POST | `/token/refresh` | Public | Rotate refresh token, get new JWT |
| POST | `/logout` | Authenticated | Revoke all refresh tokens for user |
| POST | `/password/forgot` | Public | Send password reset email |
| POST | `/password/reset` | Public | Reset password with token |
| POST | `/password/change` | Authenticated | Change password (requires current password) |
| GET | `/profile` | Authenticated | Get own profile |
| PUT | `/profile` | Authenticated | Update fullName + mobile |

### API Endpoints — AdminUserController `[/api/auth/admin/users]`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/customers` | Admin | List all customers |
| GET | `/delivery-agents` | Admin | List all delivery agents |
| GET | `/delivery-agents/approved` | Public | List active delivery agents (for partner assignment) |
| GET | `/restaurant-partners` | Admin | List all restaurant partners |
| DELETE | `/{userId}` | Admin | Delete user (not Admin) |
| PATCH | `/{userId}/toggle-active` | Admin | Activate/deactivate user |

### Login Flow

```
1. POST /login → validate credentials
   - Admin: issue JWT immediately (no OTP)
   - Others: generate 6-digit OTP, store OtpSessionToken, send OTP email
   - Returns: { requiresOtp, otpSessionToken }

2. POST /login/verify-otp → submit { otpSessionToken, otp }
   - Validate OTP + expiry (10 min)
   - Clear OTP fields
   - Issue JWT (60 min) + RefreshToken (30 days)
   - Returns: { accessToken, refreshToken, role, fullName, email, userId }
```

### JWT Claims

| Claim | Value |
|---|---|
| sub | user.Id |
| email | user.Email |
| name | user.FullName |
| jti | new Guid |
| role | role name string |

### Email Service

Uses SMTP (configured via `Email:Host/Port/Username/Password/From`).
- `SendOtpAsync` — HTML email with 6-digit OTP, 10 min expiry
- `SendPasswordResetAsync` — HTML email with reset link to `{FrontendUrl}/auth/reset-password?token=...`

---

## 6. CatalogService

**Port:** 5002 | **Database:** CatalogDb | **Project:** `Services/CatalogService`

### Architecture Layers

```
CatalogService.API          → Controllers, Program.cs
CatalogService.Application  → CatalogAppService, DTOs
CatalogService.Domain       → Restaurant, MenuItem, RestaurantApplication, DeliveryAgentApplication, IRestaurantRepository
CatalogService.Infrastructure → CatalogDbContext, RestaurantRepository
```

### Domain Entities

**Restaurant**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| OwnerId | Guid | FK → AuthService.User (cross-service, no EF FK) |
| Name | string | |
| Address | string | |
| City | string | |
| Pincode | string | |
| CuisineType | string | |
| Gst | string | GST registration number |
| Fssai | string | FSSAI license number |
| ImageUrl | string? | served from wwwroot |
| Rating | double | rolling average |
| TotalRatings | int | count for rolling average |
| IsActive | bool | default true |
| IsOpen | bool | default true |
| PrepTimeMinutes | int | default 30 |
| MinOrderAmount | decimal(18,2) | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | |

**MenuItem**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| RestaurantId | Guid | FK → Restaurant |
| Name | string | |
| Description | string | |
| Category | string | |
| Price | decimal(18,2) | |
| ImageUrl | string? | |
| IsVeg | bool | default true |
| IsAvailable | bool | default true |
| Rating | double | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | |

**RestaurantApplication**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | applicant user |
| ApplicantName | string | |
| ApplicantEmail | string | |
| RestaurantName | string | |
| Address | string | |
| City | string | |
| Pincode | string | |
| CuisineType | string | |
| Gst | string | |
| Fssai | string | |
| Status | ApplicationStatus | stored as string |
| RejectionReason | string? | |
| AppliedAt | DateTime | |
| ReviewedAt | DateTime? | |
| RestaurantId | Guid? | set when approved |

**DeliveryAgentApplication**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | applicant user |
| ApplicantName | string | |
| ApplicantEmail | string | |
| Location | string | |
| AadhaarNumber | string | |
| VehicleType | string | |
| VehicleNumber | string | |
| LicenseNumber | string | |
| Status | ApplicationStatus | stored as string |
| RejectionReason | string? | |
| AppliedAt | DateTime | |
| ReviewedAt | DateTime? | |

**ApplicationStatus enum:** `Pending`, `Approved`, `Rejected`

### API Endpoints — CatalogController `[/api/catalog]` (Public)

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/restaurants` | Public | List active restaurants (filter: city, cuisine, isOpen) |
| GET | `/restaurants/search?q=` | Public | Search restaurants by name/cuisine |
| GET | `/menu/search?q=` | Public | Search menu items by name/category/description |
| GET | `/restaurants/{id}` | Public | Restaurant detail + full menu |
| GET | `/restaurants/{id}/menu` | Public | Menu items for a restaurant |
| POST | `/restaurants/{id}/rate` | Authenticated | Submit star rating (1–5) |

### API Endpoints — RestaurantPartnerController `[/api/catalog/partner]`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/restaurants` | RestaurantPartner | My restaurants |
| PUT | `/restaurants/{id}` | RestaurantPartner | Update restaurant details |
| PATCH | `/restaurants/{id}/status?isOpen=` | RestaurantPartner | Toggle open/closed |
| POST | `/restaurants/{id}/image` | RestaurantPartner | Upload restaurant image |
| DELETE | `/restaurants/{id}` | RestaurantPartner | Delete own restaurant |
| POST | `/restaurants/{id}/menu` | RestaurantPartner | Add menu item |
| PUT | `/menu/{itemId}` | RestaurantPartner | Update menu item |
| DELETE | `/menu/{itemId}` | RestaurantPartner | Delete menu item |
| POST | `/menu/{itemId}/image` | RestaurantPartner | Upload menu item image |
| POST | `/applications` | RestaurantPartner | Submit restaurant onboarding application |
| GET | `/applications/mine` | RestaurantPartner | Get own application status |

### API Endpoints — DeliveryAgentController `[/api/catalog/delivery-agent]`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/applications` | DeliveryAgent | Submit delivery agent application |
| GET | `/applications/mine` | DeliveryAgent | Get own application status |

### API Endpoints — AdminApprovalController `[/api/catalog/admin/approvals]`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/restaurants` | Admin | List restaurant applications (filter by status) |
| POST | `/restaurants/{id}/review` | Admin | Approve/reject restaurant application |
| DELETE | `/restaurants/{id}` | Admin | Delete a restaurant |
| GET | `/delivery-agents` | Admin | List agent applications (filter by status) |
| POST | `/delivery-agents/{id}/review` | Admin | Approve/reject agent application |

### Business Rules

- When a RestaurantApplication is **Approved**, a `Restaurant` record is automatically created and `RestaurantApplication.RestaurantId` is set.
- A partner cannot submit a new application if one is already `Pending`.
- An agent cannot apply again if already `Approved`.
- Images are saved to `wwwroot/restaurants/{id}.ext` and `wwwroot/menu/{id}.ext` and served as static files.
- Rating uses a rolling average: `(rating * totalRatings + stars) / (totalRatings + 1)`.

---

## 7. OrderService

**Port:** 5003 | **Database:** OrderDb | **Project:** `Services/OrderService`

### Architecture Layers

```
OrderService.API          → CartController, OrderController, Program.cs
OrderService.Application  → OrderAppService, DTOs
OrderService.Domain       → Order, OrderItem, OrderStatusHistory, Cart, CartItem, IOrderRepository, ICartRepository, OrderStatus enum
OrderService.Infrastructure → OrderDbContext, OrderRepository, CartRepository, PaymentProcessedConsumer, OrderStateMachine, OrderSagaState
```

### Domain Entities

**Order**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CustomerId | Guid | FK → AuthService.User |
| RestaurantId | Guid | FK → CatalogService.Restaurant |
| RestaurantName | string | denormalized |
| Status | OrderStatus | stored as string |
| DeliveryAddress | string | |
| DeliveryInstructions | string? | |
| PromoCode | string? | |
| SubTotal | decimal(18,2) | |
| DiscountAmount | decimal(18,2) | |
| DeliveryFee | decimal(18,2) | fixed ₹30 |
| GstAmount | decimal(18,2) | 5% of subtotal |
| TotalAmount | decimal(18,2) | subtotal + gst + delivery |
| PaymentMethod | string | COD / Card / Wallet |
| PaymentId | Guid? | set after payment |
| DeliveryAgentId | Guid? | set when agent assigned |
| CancellationReason | string? | |
| CancelledBy | string? | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | |
| EstimatedDeliveryAt | DateTime? | CreatedAt + 45 min |

**OrderItem**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| OrderId | Guid | FK → Order |
| MenuItemId | Guid | FK → CatalogService.MenuItem |
| Name | string | denormalized |
| UnitPrice | decimal(18,2) | |
| Quantity | int | |
| TotalPrice | computed | UnitPrice * Quantity (not stored) |

**OrderStatusHistory**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| OrderId | Guid | FK → Order |
| Status | OrderStatus | stored as string |
| Note | string? | |
| ChangedBy | string | role name or "System" |
| ChangedAt | DateTime | |

**Cart**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CustomerId | Guid | unique index (one cart per customer) |
| RestaurantId | Guid | |
| RestaurantName | string | |
| UpdatedAt | DateTime | |

**CartItem**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CartId | Guid | FK → Cart |
| MenuItemId | Guid | |
| Name | string | |
| UnitPrice | decimal(18,2) | |
| Quantity | int | |

**OrderSagaState** (MassTransit saga persistence)
| Field | Type | Notes |
|---|---|---|
| CorrelationId | Guid | PK = OrderId |
| CurrentState | string | max 64 |
| CustomerId | Guid | |
| RestaurantId | Guid | |
| TotalAmount | decimal(18,2) | |
| PaymentMethod | string | max 32 |
| PaymentId | Guid? | |
| AgentId | Guid? | |
| CreatedAt | DateTime | |

### OrderStatus Enum (full lifecycle)

```
DraftCart → CheckoutStarted → PaymentPending → Paid
→ RestaurantAccepted → Preparing → ReadyForPickup
→ PickedUp → OutForDelivery → Delivered

Failure paths:
PaymentPending → PaymentFailed
RestaurantAccepted → RestaurantRejected
Any → CancelRequested → Cancelled
Delivered/Cancelled → RefundInitiated → Refunded
```

### API Endpoints — CartController `[/api/cart]` (Customer only)

| Method | Route | Description |
|---|---|---|
| GET | `/` | Get current cart |
| POST | `/items` | Add item to cart (clears cart if different restaurant) |
| PUT | `/items/{menuItemId}` | Update item quantity (0 = remove) |
| DELETE | `/` | Clear entire cart |

### API Endpoints — OrderController `[/api/orders]`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/checkout` | Customer | Place order from cart, publish OrderPlacedEvent |
| GET | `/{id}` | Authenticated | Get order by ID |
| GET | `/my` | Customer | My order history |
| GET | `/restaurant/{restaurantId}` | RestaurantPartner | Orders for a restaurant |
| GET | `/my-deliveries` | DeliveryAgent | Orders assigned to me |
| GET | `/` | Admin | All orders (filter by status) |
| PATCH | `/{id}/status` | Partner/Agent/Customer/Admin | Update order status (role-gated) |
| POST | `/{id}/assign-agent` | Partner/Admin | Assign delivery agent |

### Status Transition Rules (role-based)

| Role | Allowed Transitions |
|---|---|
| RestaurantPartner | RestaurantAccepted, RestaurantRejected, Preparing, ReadyForPickup |
| DeliveryAgent | PickedUp, OutForDelivery, Delivered |
| Customer | CancelRequested (only before Preparing) |
| Admin | Any status |

### Checkout Pricing Logic

```
SubTotal = sum(item.unitPrice * item.quantity)
GST      = round(SubTotal * 0.05, 2)
Delivery = 30.00 (fixed)
Total    = SubTotal + GST + Delivery
EstimatedDelivery = now + 45 minutes
```

---

## 8. PaymentService

**Port:** 5004 | **Database:** PaymentDb | **Project:** `Services/PaymentService`

### Architecture Layers

```
PaymentService.API          → PaymentController, Program.cs
PaymentService.Application  → PaymentAppService, DTOs
PaymentService.Domain       → Payment, IPaymentRepository
PaymentService.Infrastructure → PaymentDbContext, PaymentRepository
```

### Domain Entities

**Payment**
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| OrderId | Guid | indexed |
| CustomerId | Guid | |
| Amount | decimal(18,2) | |
| Method | string | COD / Card / Wallet |
| Status | string | Pending / Success / Failed / Refunded |
| FailureReason | string? | |
| TransactionId | string? | `TXN-{16 chars}` on success |
| CreatedAt | DateTime | |
| ProcessedAt | DateTime? | |

### API Endpoints — PaymentController `[/api/payments]`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/simulate` | Authenticated | Simulate payment (success or failure) |
| POST | `/refund` | Admin | Refund a successful payment |
| GET | `/order/{orderId}` | Authenticated | Get payment for an order |
| GET | `/` | Admin | All payments |

### Payment Flow

```
1. Customer places order → OrderService publishes OrderPlacedEvent
2. Frontend calls POST /gateway/payments/simulate with { orderId, customerId, amount, method }
3. PaymentService creates Payment record
4. PaymentService publishes PaymentProcessedEvent { orderId, paymentId, success, failureReason }
5. OrderService.PaymentProcessedConsumer receives event:
   - Success → Order.Status = Paid, Order.PaymentId = paymentId
   - Failure → Order.Status = PaymentFailed
6. OrderSaga also receives PaymentProcessedEvent and transitions state
```

### Refund Flow

```
POST /payments/refund { paymentId, reason }
→ Payment.Status = "Refunded"
→ Publishes RefundInitiatedEvent { orderId, paymentId, amount, reason }
```

---

## 9. AdminService

**Port:** 5005 | **No own database** | **Project:** `Services/AdminService`

AdminService is an aggregator — it has no database of its own. It calls other services over HTTP using named `HttpClient` instances, forwarding the caller's JWT Bearer token.

### Architecture Layers

```
AdminService.API          → AdminController, Program.cs
AdminService.Application  → AdminAppService, DTOs
```

### HTTP Client Targets

| Named Client | Base URL (config) |
|---|---|
| OrderService | Services:OrderService |
| CatalogService | Services:CatalogService |
| PaymentService | Services:PaymentService |
| AuthService | Services:AuthService |

### API Endpoints — AdminController `[/api/admin]` (Admin only)

| Method | Route | Description |
|---|---|---|
| GET | `/dashboard` | Aggregated stats (orders, revenue, restaurants, pending apps, users) |
| GET | `/reports/sales?from=&to=` | Daily sales report (grouped by date) |
| GET | `/reports/partners` | Partner restaurant listing with ratings |

### Dashboard Data Sources

| Metric | Source |
|---|---|
| TotalOrders, TotalRevenue | GET /api/orders (OrderService) |
| TodayOrders, TodayRevenue | filtered from orders by today's date |
| ActiveRestaurants | GET /api/catalog/restaurants (CatalogService) |
| PendingRestaurantApplications | GET /api/catalog/admin/approvals/restaurants |
| PendingAgentApplications | GET /api/catalog/admin/approvals/delivery-agents |
| TotalUsers | customers + agents + partners from AuthService |

---

## 10. Shared Library

**Project:** `Services/Shared/FoodDelivery.Shared`

Referenced by all services via `ProjectReference`.

### Contents

**Exceptions** (`FoodDelivery.Shared.Exceptions`)

| Class | HTTP Status | Description |
|---|---|---|
| AppException | 400 | Base exception |
| NotFoundException | 404 | Resource not found |
| ConflictException | 409 | Duplicate / conflict |
| BusinessRuleException | 422 | Business rule violation |
| ForbiddenException | 403 | Access denied |
| InvalidStatusTransitionException | 422 | Invalid order status transition |

**Middleware** (`FoodDelivery.Shared.Middleware`)

`GlobalExceptionMiddleware` — catches `AppException` (returns proper status code) and unhandled `Exception` (returns 500). Used by all 5 services.

**Events** (`FoodDelivery.Shared.Events`) — see Section 11.

---

## 11. RabbitMQ — Events & Saga

### Event Definitions (all in `FoodDelivery.Shared.Events`)

| Event | Fields | Publisher | Consumer |
|---|---|---|---|
| `OrderPlacedEvent` | OrderId, CustomerId, RestaurantId, TotalAmount, PaymentMethod, PlacedAt | OrderService | OrderSaga |
| `PaymentProcessedEvent` | OrderId, PaymentId, Success, FailureReason, ProcessedAt | PaymentService | OrderService (consumer + saga) |
| `OrderAcceptedEvent` | OrderId, RestaurantId, PrepTimeMinutes, AcceptedAt | (future) | OrderSaga |
| `OrderRejectedEvent` | OrderId, RestaurantId, Reason, RejectedAt | (future) | OrderSaga |
| `OrderReadyEvent` | OrderId, RestaurantId, ReadyAt | (future) | OrderSaga |
| `DeliveryAssignedEvent` | OrderId, AgentId, AssignedAt | OrderService | OrderSaga |
| `OrderPickedUpEvent` | OrderId, AgentId, PickedUpAt | (future) | OrderSaga |
| `OrderDeliveredEvent` | OrderId, AgentId, DeliveredAt | OrderService | OrderSaga |
| `OrderCancelledEvent` | OrderId, Reason, CancelledBy, CancelledAt | OrderService | OrderSaga |
| `RefundInitiatedEvent` | OrderId, PaymentId, Amount, Reason, InitiatedAt | PaymentService | (future) |

### MassTransit Saga — OrderStateMachine

All events are correlated by `OrderId`.

**States:**

```
Initial → AwaitingPayment → AwaitingRestaurant → Preparing
       → AwaitingPickup → InDelivery → Completed (Final)
       → Failed (terminal)
```

**Transitions:**

| From State | Event | Condition | To State |
|---|---|---|---|
| Initial | OrderPlacedEvent | — | AwaitingPayment |
| AwaitingPayment | PaymentProcessedEvent | Success=true | AwaitingRestaurant |
| AwaitingPayment | PaymentProcessedEvent | Success=false | Failed |
| AwaitingPayment | OrderCancelledEvent | — | Failed |
| AwaitingRestaurant | OrderAcceptedEvent | — | Preparing |
| AwaitingRestaurant | OrderRejectedEvent | — | Failed |
| AwaitingRestaurant | OrderCancelledEvent | — | Failed |
| Preparing | OrderReadyEvent | — | AwaitingPickup |
| AwaitingPickup | DeliveryAssignedEvent | — | (stays, sets AgentId) |
| AwaitingPickup | OrderPickedUpEvent | — | InDelivery |
| InDelivery | OrderDeliveredEvent | — | Completed (Finalized) |

**Saga State Persistence:** EF Core with Pessimistic concurrency, stored in `OrderSagaStates` table in OrderDb.

### PaymentProcessedConsumer

Runs in OrderService. On receiving `PaymentProcessedEvent`:
- Updates `Order.Status` to `Paid` or `PaymentFailed`
- Sets `Order.PaymentId`
- Inserts a row into `OrderStatusHistories`
- Uses raw SQL to avoid EF tracking conflicts

---

## 12. Database Schemas

Each service has its own isolated SQL Server database. There are no cross-database foreign keys — references across services use Guid IDs only.

---

### AuthDb

**Tables (ASP.NET Core Identity + custom)**

```
AspNetUsers (User)
  Id                      uniqueidentifier  PK
  FullName                nvarchar(200)     NOT NULL
  Mobile                  nvarchar(20)      NOT NULL, UNIQUE
  Role                    nvarchar(50)      NOT NULL  (enum as string)
  IsActive                bit               DEFAULT 1
  ProfileImageUrl         nvarchar(max)     NULL
  OtpCode                 nvarchar(max)     NULL
  OtpExpiry               datetime2         NULL
  OtpSessionToken         nvarchar(max)     NULL
  PasswordResetToken      nvarchar(max)     NULL
  PasswordResetTokenExpiry datetime2        NULL
  CreatedAt               datetime2         NOT NULL
  UpdatedAt               datetime2         NULL
  -- Standard Identity columns: UserName, NormalizedUserName, Email,
  --   NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp,
  --   ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
  --   TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount

RefreshTokens
  Id          uniqueidentifier  PK
  Token       nvarchar(max)     NOT NULL
  ExpiresAt   datetime2         NOT NULL
  IsRevoked   bit               DEFAULT 0
  CreatedAt   datetime2         NOT NULL
  UserId      uniqueidentifier  FK → AspNetUsers.Id

AspNetRoles               (Identity roles: Admin, Customer, RestaurantPartner, DeliveryAgent)
AspNetUserRoles           (Identity user-role mapping)
AspNetRoleClaims
AspNetUserClaims
AspNetUserLogins
AspNetUserTokens
```

---

### CatalogDb

```
Restaurants
  Id              uniqueidentifier  PK
  OwnerId         uniqueidentifier  NOT NULL  (ref to AuthDb.Users, no FK)
  Name            nvarchar(max)     NOT NULL
  Address         nvarchar(max)     NOT NULL
  City            nvarchar(max)     NOT NULL
  Pincode         nvarchar(max)     NOT NULL
  CuisineType     nvarchar(max)     NOT NULL
  Gst             nvarchar(max)     NOT NULL
  Fssai           nvarchar(max)     NOT NULL
  ImageUrl        nvarchar(max)     NULL
  Rating          float             DEFAULT 0
  TotalRatings    int               DEFAULT 0
  IsActive        bit               DEFAULT 1
  IsOpen          bit               DEFAULT 1
  PrepTimeMinutes int               DEFAULT 30
  MinOrderAmount  decimal(18,2)     DEFAULT 0
  CreatedAt       datetime2         NOT NULL
  UpdatedAt       datetime2         NULL

MenuItems
  Id            uniqueidentifier  PK
  RestaurantId  uniqueidentifier  FK → Restaurants.Id
  Name          nvarchar(max)     NOT NULL
  Description   nvarchar(max)     NOT NULL
  Category      nvarchar(max)     NOT NULL
  Price         decimal(18,2)     NOT NULL
  ImageUrl      nvarchar(max)     NULL
  IsVeg         bit               DEFAULT 1
  IsAvailable   bit               DEFAULT 1
  Rating        float             DEFAULT 0
  CreatedAt     datetime2         NOT NULL
  UpdatedAt     datetime2         NULL

RestaurantApplications
  Id               uniqueidentifier  PK
  UserId           uniqueidentifier  NOT NULL
  ApplicantName    nvarchar(max)     NOT NULL
  ApplicantEmail   nvarchar(max)     NOT NULL
  RestaurantName   nvarchar(max)     NOT NULL
  Address          nvarchar(max)     NOT NULL
  City             nvarchar(max)     NOT NULL
  Pincode          nvarchar(max)     NOT NULL
  CuisineType      nvarchar(max)     NOT NULL
  Gst              nvarchar(max)     NOT NULL
  Fssai            nvarchar(max)     NOT NULL
  Status           nvarchar(max)     NOT NULL  (Pending/Approved/Rejected)
  RejectionReason  nvarchar(max)     NULL
  AppliedAt        datetime2         NOT NULL
  ReviewedAt       datetime2         NULL
  RestaurantId     uniqueidentifier  NULL  (set on approval)

DeliveryAgentApplications
  Id               uniqueidentifier  PK
  UserId           uniqueidentifier  NOT NULL
  ApplicantName    nvarchar(max)     NOT NULL
  ApplicantEmail   nvarchar(max)     NOT NULL
  Location         nvarchar(max)     NOT NULL
  AadhaarNumber    nvarchar(max)     NOT NULL
  VehicleType      nvarchar(max)     NOT NULL
  VehicleNumber    nvarchar(max)     NOT NULL
  LicenseNumber    nvarchar(max)     NOT NULL
  Status           nvarchar(max)     NOT NULL  (Pending/Approved/Rejected)
  RejectionReason  nvarchar(max)     NULL
  AppliedAt        datetime2         NOT NULL
  ReviewedAt       datetime2         NULL
```

---

### OrderDb

```
Orders
  Id                   uniqueidentifier  PK
  CustomerId           uniqueidentifier  NOT NULL
  RestaurantId         uniqueidentifier  NOT NULL
  RestaurantName       nvarchar(max)     NOT NULL
  Status               nvarchar(max)     NOT NULL  (enum as string)
  DeliveryAddress      nvarchar(max)     NOT NULL
  DeliveryInstructions nvarchar(max)     NULL
  PromoCode            nvarchar(max)     NULL
  SubTotal             decimal(18,2)     NOT NULL
  DiscountAmount       decimal(18,2)     NOT NULL
  DeliveryFee          decimal(18,2)     NOT NULL
  GstAmount            decimal(18,2)     NOT NULL
  TotalAmount          decimal(18,2)     NOT NULL
  PaymentMethod        nvarchar(max)     NOT NULL
  PaymentId            uniqueidentifier  NULL
  DeliveryAgentId      uniqueidentifier  NULL
  CancellationReason   nvarchar(max)     NULL
  CancelledBy          nvarchar(max)     NULL
  CreatedAt            datetime2         NOT NULL
  UpdatedAt            datetime2         NULL
  EstimatedDeliveryAt  datetime2         NULL

OrderItems
  Id          uniqueidentifier  PK
  OrderId     uniqueidentifier  FK → Orders.Id
  MenuItemId  uniqueidentifier  NOT NULL
  Name        nvarchar(max)     NOT NULL
  UnitPrice   decimal(18,2)     NOT NULL
  Quantity    int               NOT NULL

OrderStatusHistories
  Id         uniqueidentifier  PK
  OrderId    uniqueidentifier  FK → Orders.Id
  Status     nvarchar(max)     NOT NULL  (enum as string)
  Note       nvarchar(max)     NULL
  ChangedBy  nvarchar(max)     NOT NULL
  ChangedAt  datetime2         NOT NULL

Carts
  Id             uniqueidentifier  PK
  CustomerId     uniqueidentifier  NOT NULL, UNIQUE INDEX
  RestaurantId   uniqueidentifier  NOT NULL
  RestaurantName nvarchar(max)     NOT NULL
  UpdatedAt      datetime2         NOT NULL

CartItems
  Id          uniqueidentifier  PK
  CartId      uniqueidentifier  FK → Carts.Id
  MenuItemId  uniqueidentifier  NOT NULL
  Name        nvarchar(max)     NOT NULL
  UnitPrice   decimal(18,2)     NOT NULL
  Quantity    int               NOT NULL

OrderSagaStates
  CorrelationId  uniqueidentifier  PK  (= OrderId)
  CurrentState   nvarchar(64)      NOT NULL
  CustomerId     uniqueidentifier  NOT NULL
  RestaurantId   uniqueidentifier  NOT NULL
  TotalAmount    decimal(18,2)     NOT NULL
  PaymentMethod  nvarchar(32)      NOT NULL
  PaymentId      uniqueidentifier  NULL
  AgentId        uniqueidentifier  NULL
  CreatedAt      datetime2         NOT NULL
```

---

### PaymentDb

```
Payments
  Id             uniqueidentifier  PK
  OrderId        uniqueidentifier  NOT NULL, INDEX
  CustomerId     uniqueidentifier  NOT NULL
  Amount         decimal(18,2)     NOT NULL
  Method         nvarchar(max)     NOT NULL  (COD/Card/Wallet)
  Status         nvarchar(max)     NOT NULL  (Pending/Success/Failed/Refunded)
  FailureReason  nvarchar(max)     NULL
  TransactionId  nvarchar(max)     NULL
  CreatedAt      datetime2         NOT NULL
  ProcessedAt    datetime2         NULL
```

---

## 13. Frontend — Angular

**Framework:** Angular 17 (standalone components, Signals)
**Styling:** Tailwind CSS
**Base URL:** `http://localhost:5000` (API Gateway)

### Project Structure

```
Frontend/src/app/
├── app.routes.ts           ← All routes with lazy loading
├── app.config.ts           ← provideRouter, provideHttpClient, interceptors
├── core/
│   ├── guards/
│   │   ├── auth.guard.ts   ← Redirects to /auth/login if not logged in
│   │   ├── role.guard.ts   ← Checks route data.roles against user role
│   │   └── home.guard.ts   ← Redirects logged-in users to their dashboard
│   ├── interceptors/
│   │   └── auth.interceptor.ts  ← Attaches Bearer token to all requests
│   ├── models/
│   │   ├── auth.models.ts       ← LoginRequest, RegisterRequest, AuthResponse, User
│   │   └── restaurant.models.ts ← Restaurant, MenuItem, Cart, CartItem, Order, OrderItem
│   └── services/
│       ├── auth.service.ts      ← Signals-based auth state, JWT session management
│       ├── catalog.service.ts   ← Restaurant/menu API calls
│       ├── cart.service.ts      ← Cart state (Signals), cart API calls
│       ├── order.service.ts     ← Order API calls
│       ├── partner.service.ts   ← Partner restaurant/menu/application API calls
│       ├── admin.service.ts     ← Admin dashboard/approvals/users API calls
│       └── user.service.ts      ← Profile get/update, change password
├── shared/
│   ├── components/
│   │   ├── navbar/          ← Role-aware navigation
│   │   ├── footer/
│   │   ├── profile/         ← Shared profile component (used by all roles)
│   │   ├── toast/           ← Toast notification component
│   │   └── restaurant-card/ ← Restaurant card for listing
│   └── services/
│       └── toast.service.ts
└── features/
    ├── auth/
    │   ├── login/           ← Email + password → OTP flow
    │   ├── register/        ← Role selection + form
    │   ├── verify-otp/      ← OTP input
    │   ├── forgot-password/
    │   └── reset-password/
    ├── home/                ← Landing page with restaurant search
    ├── restaurants/
    │   ├── restaurant-list/ ← Browse/filter restaurants
    │   └── restaurant-detail/ ← Menu + add to cart
    ├── customer/
    │   ├── cart/            ← Cart view + checkout button
    │   ├── checkout/        ← Address + payment method + place order
    │   └── orders/          ← Order history + status tracking
    ├── partner/
    │   ├── dashboard/       ← Partner overview
    │   ├── restaurants/     ← Manage restaurants + menus
    │   ├── orders/          ← Incoming orders + status updates
    │   └── apply/           ← Restaurant onboarding application
    ├── delivery/
    │   └── dashboard/       ← Assigned deliveries + status updates
    └── admin/
        ├── dashboard/       ← Stats overview
        ├── users/           ← User management
        ├── approvals/       ← Restaurant + agent application review
        └── partner-report/  ← Partner analytics
```

### Routing

| Path | Component | Guards | Roles |
|---|---|---|---|
| `/` | HomeComponent | homeGuard | — |
| `/auth/login` | LoginComponent | — | — |
| `/auth/register` | RegisterComponent | — | — |
| `/auth/verify-otp` | VerifyOtpComponent | — | — |
| `/auth/forgot-password` | ForgotPasswordComponent | — | — |
| `/auth/reset-password` | ResetPasswordComponent | — | — |
| `/restaurants` | RestaurantListComponent | — | — |
| `/restaurants/:id` | RestaurantDetailComponent | — | — |
| `/customer/cart` | CartComponent | authGuard, roleGuard | Customer |
| `/customer/checkout` | CheckoutComponent | authGuard, roleGuard | Customer |
| `/customer/orders` | OrdersComponent | authGuard, roleGuard | Customer |
| `/customer/profile` | ProfileComponent (shared) | authGuard, roleGuard | Customer |
| `/partner/dashboard` | PartnerDashboardComponent | authGuard, roleGuard | RestaurantPartner |
| `/partner/restaurants` | PartnerRestaurantsComponent | authGuard, roleGuard | RestaurantPartner |
| `/partner/orders` | PartnerOrdersComponent | authGuard, roleGuard | RestaurantPartner |
| `/partner/apply` | PartnerApplyComponent | authGuard, roleGuard | RestaurantPartner |
| `/partner/profile` | ProfileComponent (shared) | authGuard, roleGuard | RestaurantPartner |
| `/admin/dashboard` | AdminDashboardComponent | authGuard, roleGuard | Admin |
| `/admin/users` | AdminUsersComponent | authGuard, roleGuard | Admin |
| `/admin/approvals` | AdminApprovalsComponent | authGuard, roleGuard | Admin |
| `/admin/partner-report` | AdminPartnerReportComponent | authGuard, roleGuard | Admin |
| `/admin/profile` | ProfileComponent (shared) | authGuard, roleGuard | Admin |
| `/delivery/dashboard` | DeliveryDashboardComponent | authGuard, roleGuard | DeliveryAgent |
| `/delivery/profile` | ProfileComponent (shared) | authGuard, roleGuard | DeliveryAgent |

### Auth State (AuthService — Signals)

```typescript
_user  = signal<User | null>   // persisted in localStorage
_token = signal<string | null> // persisted in localStorage
isLoggedIn = computed(() => !!_token())
role       = computed(() => _user()?.role ?? null)
```

Session is stored in `localStorage`: `accessToken`, `refreshToken`, `user` (JSON).

### Frontend API Service → Gateway Mapping

| Service | Base URL |
|---|---|
| AuthService | `{apiUrl}/gateway/auth` |
| CatalogService | `{apiUrl}/gateway/catalog` |
| CartService | `{apiUrl}/gateway/cart` |
| OrderService | `{apiUrl}/gateway/orders` |
| PaymentService | `{apiUrl}/gateway/payments` |
| AdminService (catalog) | `{apiUrl}/gateway/catalog/admin/approvals` |
| AdminService (auth) | `{apiUrl}/gateway/auth/admin/users` |
| AdminService (admin) | `{apiUrl}/gateway/admin` |

---

## 14. Cross-Cutting Concerns

### Authentication & Authorization

- All services validate the same JWT (shared `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` config).
- JWT is issued by AuthService only. Other services validate but never issue tokens.
- Access token lifetime: configurable via `Jwt:AccessTokenMinutes` (default 60 min).
- Refresh token lifetime: 30 days, rotated on each use, revoked on logout.

### Error Handling

`GlobalExceptionMiddleware` (from shared library) is registered first in every service pipeline:
- `AppException` subclasses → return their specific HTTP status code
- Unhandled exceptions → 500 in production, full message in development

### Logging

All services use Serilog with:
- Console sink
- Rolling file sink: `logs/{servicename}-.log` (daily rotation)
- Log context enrichment

### CORS

- AuthService: `AllowFrontend` policy → `http://localhost:4200`
- Gateway: default policy → `AllowAnyOrigin` (for development)

### Static Files

CatalogService serves images from `wwwroot/` via `UseStaticFiles()`:
- Restaurant images: `/restaurants/{restaurantId}.{ext}`
- Menu item images: `/menu/{itemId}.{ext}`

### Database Migrations

All services with a database run `db.Database.Migrate()` on startup. AuthService also seeds the default Admin user (`admin@gmail.com` / `Admin@1234`) and all four roles.

---

## 15. User Roles & Permissions

| Role | Registration | Key Capabilities |
|---|---|---|
| Customer | Self-register | Browse restaurants, manage cart, place orders, track orders, rate restaurants |
| RestaurantPartner | Self-register | Apply for restaurant, manage restaurants/menus, view/update incoming orders, assign delivery agents |
| DeliveryAgent | Self-register | Apply as agent, view assigned deliveries, update delivery status |
| Admin | Seeded only | Full user management, approve/reject applications, view all orders, sales reports, partner reports, refund payments |

---

## 16. Order Lifecycle Flow

```
Customer                  OrderService           PaymentService         RestaurantPartner       DeliveryAgent
   │                           │                       │                       │                     │
   │── Add items to cart ──────►│                       │                       │                     │
   │                           │                       │                       │                     │
   │── POST /checkout ─────────►│                       │                       │                     │
   │                           │── OrderPlacedEvent ───►│ (RabbitMQ/Saga)       │                     │
   │                           │   Status: PaymentPending                       │                     │
   │                           │                       │                       │                     │
   │── POST /payments/simulate ─────────────────────────►│                      │                     │
   │                           │                       │── PaymentProcessedEvent►│ (RabbitMQ)         │
   │                           │◄── PaymentProcessedConsumer ──────────────────│                     │
   │                           │   Status: Paid         │                       │                     │
   │                           │                       │                       │                     │
   │                           │                       │         Partner views order                  │
   │                           │◄── PATCH /orders/{id}/status (RestaurantAccepted) ─────────────────│
   │                           │   Status: RestaurantAccepted                  │                     │
   │                           │                       │                       │                     │
   │                           │◄── POST /orders/{id}/assign-agent ────────────►│                    │
   │                           │   DeliveryAgentId set  │                       │                     │
   │                           │                       │                       │                     │
   │                           │◄── PATCH /status (Preparing) ─────────────────►│                   │
   │                           │◄── PATCH /status (ReadyForPickup) ─────────────►│                  │
   │                           │                       │                       │                     │
   │                           │◄── PATCH /status (PickedUp) ──────────────────────────────────────►│
   │                           │◄── PATCH /status (OutForDelivery) ────────────────────────────────►│
   │                           │◄── PATCH /status (Delivered) ─────────────────────────────────────►│
   │                           │   OrderDeliveredEvent published               │                     │
   │                           │   Saga → Completed                            │                     │
```

### Cancellation Flow

```
Customer → PATCH /orders/{id}/status { newStatus: CancelRequested }
  (only allowed before Preparing)
Admin → PATCH /orders/{id}/status { newStatus: Cancelled }
  → OrderCancelledEvent published
  → Admin can then POST /payments/refund if payment was made
  → RefundInitiatedEvent published
```

---

*End of Architecture Documentation*
