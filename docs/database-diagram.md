# Database Diagram — FoodDelivery

> 4 separate SQL Server databases. No cross-database FK constraints — only logical Guid references.

---

## AuthDb

```mermaid
erDiagram
    AspNetUsers {
        guid Id PK
        string FullName
        string Email
        string Mobile "UNIQUE"
        string Role "Customer / Partner / Admin / Agent"
        bool IsActive
        string OtpCode "nullable"
        datetime OtpExpiry "nullable"
        string OtpSessionToken "nullable"
        string PasswordResetToken "nullable"
        datetime PasswordResetTokenExpiry "nullable"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    RefreshTokens {
        guid Id PK
        string Token
        datetime ExpiresAt
        bool IsRevoked
        datetime CreatedAt
        guid UserId FK
    }

    AspNetRoles {
        guid Id PK
        string Name
    }

    AspNetUserRoles {
        guid UserId FK
        guid RoleId FK
    }

    AspNetUsers ||--o{ RefreshTokens : "has"
    AspNetUsers ||--o{ AspNetUserRoles : "assigned"
    AspNetRoles ||--o{ AspNetUserRoles : "assigned to"
```

---

## CatalogDb

```mermaid
erDiagram
    Restaurants {
        guid Id PK
        guid OwnerId "ref AuthDb.Users"
        string Name
        string City
        string CuisineType
        string Gst
        string Fssai
        string ImageUrl "nullable"
        float Rating
        int TotalRatings
        bool IsActive
        bool IsOpen
        int PrepTimeMinutes
        decimal MinOrderAmount
        datetime CreatedAt
    }

    MenuItems {
        guid Id PK
        guid RestaurantId FK
        string Name
        string Category
        decimal Price
        string ImageUrl "nullable"
        bool IsVeg
        bool IsAvailable
        float Rating
        datetime CreatedAt
    }

    RestaurantApplications {
        guid Id PK
        guid UserId "ref AuthDb.Users"
        string RestaurantName
        string City
        string CuisineType
        string Status "Pending / Approved / Rejected"
        string RejectionReason "nullable"
        datetime AppliedAt
        datetime ReviewedAt "nullable"
        guid RestaurantId "nullable - set on approval"
    }

    DeliveryAgentApplications {
        guid Id PK
        guid UserId "ref AuthDb.Users"
        string Location
        string VehicleType
        string VehicleNumber
        string LicenseNumber
        string Status "Pending / Approved / Rejected"
        string RejectionReason "nullable"
        datetime AppliedAt
        datetime ReviewedAt "nullable"
    }

    Restaurants ||--o{ MenuItems : "has"
```

---

## OrderDb

```mermaid
erDiagram
    Orders {
        guid Id PK
        guid CustomerId "ref AuthDb.Users"
        guid RestaurantId "ref CatalogDb.Restaurants"
        string RestaurantName "denormalized"
        string CustomerName "denormalized"
        string CustomerMobile "denormalized"
        string Status
        string DeliveryAddress
        decimal SubTotal
        decimal GstAmount
        decimal DeliveryFee
        decimal TotalAmount
        string PaymentMethod
        guid PaymentId "nullable"
        guid DeliveryAgentId "nullable"
        datetime CreatedAt
        datetime EstimatedDeliveryAt "nullable"
    }

    OrderItems {
        guid Id PK
        guid OrderId FK
        guid MenuItemId "ref CatalogDb.MenuItems"
        string Name "denormalized"
        decimal UnitPrice
        int Quantity
    }

    OrderStatusHistories {
        guid Id PK
        guid OrderId FK
        string Status
        string Note "nullable"
        string ChangedBy
        datetime ChangedAt
    }

    Carts {
        guid Id PK
        guid CustomerId "UNIQUE"
        guid RestaurantId "ref CatalogDb.Restaurants"
        string RestaurantName "denormalized"
        datetime UpdatedAt
    }

    CartItems {
        guid Id PK
        guid CartId FK
        guid MenuItemId "ref CatalogDb.MenuItems"
        string Name "denormalized"
        decimal UnitPrice
        int Quantity
    }

    OrderSagaStates {
        guid CorrelationId PK "= OrderId"
        string CurrentState
        guid CustomerId
        guid RestaurantId
        decimal TotalAmount
        string PaymentMethod
        guid PaymentId "nullable"
        guid AgentId "nullable"
        datetime CreatedAt
    }

    Orders ||--o{ OrderItems : "has"
    Orders ||--o{ OrderStatusHistories : "has"
    Carts ||--o{ CartItems : "has"
```

---

## PaymentDb

```mermaid
erDiagram
    Payments {
        guid Id PK
        guid OrderId "INDEX - ref OrderDb.Orders"
        guid CustomerId "ref AuthDb.Users"
        decimal Amount
        string Method "COD / UPI / Card"
        string Status "Pending / Success / Failed / Refunded"
        string FailureReason "nullable"
        string TransactionId "nullable"
        datetime CreatedAt
        datetime ProcessedAt "nullable"
    }
```

---

## Cross-Service References

```mermaid
flowchart LR
    subgraph A["AuthDb"]
        U["AspNetUsers"]
    end
    subgraph C["CatalogDb"]
        R["Restaurants"]
        M["MenuItems"]
    end
    subgraph O["OrderDb"]
        OR["Orders"]
        OI["OrderItems"]
        CA["Carts"]
        CI["CartItems"]
    end
    subgraph P["PaymentDb"]
        PA["Payments"]
    end

    U -.->|OwnerId| R
    U -.->|CustomerId| OR
    U -.->|DeliveryAgentId| OR
    U -.->|CustomerId| PA
    R -.->|RestaurantId| OR
    R -.->|RestaurantId| CA
    M -.->|MenuItemId| OI
    M -.->|MenuItemId| CI
    OR -.->|OrderId| PA
```
