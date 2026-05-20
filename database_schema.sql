-- ============================================================
--  CARGO EXPRESO · PROGRAMA DE PUNTOS
--  SQL Server 2019+
--  Version   : 2.0.0  (Phase 2 - Complete Schema)
--  Generated : 2026-05-20
--  Encoding  : UTF-8
--
--  Execution order: run top to bottom in a single batch.
--  Re-runnable: DROP statements at the top handle idempotency.
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CargoExpresoPuntos')
    CREATE DATABASE CargoExpresoPuntos COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE CargoExpresoPuntos;
GO

SET NOCOUNT ON;
GO

-- ============================================================
-- SECTION 0: CLEANUP — drop FKs first, then tables
--             (safe to re-run on an existing DB)
-- ============================================================

-- Drop FK constraints (prevents FK-violation during DROP TABLE)
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
             + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(13)
FROM sys.foreign_keys;
EXEC sp_executesql @sql;
GO

-- Drop tables in reverse dependency order
DROP TABLE IF EXISTS FraudAlerts;
DROP TABLE IF EXISTS LoginAttempts;
DROP TABLE IF EXISTS AuditLogs;
DROP TABLE IF EXISTS SystemConfigurationAudit;
DROP TABLE IF EXISTS RefreshTokens;
DROP TABLE IF EXISTS PointsTransactions;
DROP TABLE IF EXISTS RedemptionQrCodes;
DROP TABLE IF EXISTS RedemptionRequests;
DROP TABLE IF EXISTS Shipments;
DROP TABLE IF EXISTS PointsAccounts;
DROP TABLE IF EXISTS CustomerProfiles;
DROP TABLE IF EXISTS Customers;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Branches;
DROP TABLE IF EXISTS SystemConfigurations;
DROP TABLE IF EXISTS Countries;
GO

-- Drop stored procedures
DROP PROCEDURE IF EXISTS sp_ScanShipment;
DROP PROCEDURE IF EXISTS sp_ApplyRedemption;
DROP PROCEDURE IF EXISTS sp_ApplyRegistrationBonus;
DROP PROCEDURE IF EXISTS sp_ExpireShipments;
DROP PROCEDURE IF EXISTS sp_ExpireQrCodes;
GO


-- ============================================================
-- SECTION 1: REFERENCE TABLES
-- ============================================================

-- ------------------------------------------------------------
-- Countries
-- Supports multi-country deployment across Central America.
-- ------------------------------------------------------------
CREATE TABLE Countries (
    Id          TINYINT         NOT NULL,
    Code        CHAR(2)         NOT NULL,   -- ISO 3166-1 alpha-2
    Name        NVARCHAR(100)   NOT NULL,
    Currency    CHAR(3)         NOT NULL,   -- ISO 4217
    TimeZone    VARCHAR(50)     NOT NULL,   -- IANA tz identifier
    IsActive    BIT             NOT NULL    CONSTRAINT DF_Countries_IsActive DEFAULT 1,

    CONSTRAINT PK_Countries             PRIMARY KEY (Id),
    CONSTRAINT UQ_Countries_Code        UNIQUE (Code),
    CONSTRAINT CK_Countries_Code        CHECK (Code IN ('GT','SV','HN','CR')),
    CONSTRAINT CK_Countries_Currency    CHECK (Currency IN ('GTQ','USD','HNL','CRC'))
);
GO

-- ------------------------------------------------------------
-- SystemConfigurations
-- ALL business rules are stored here — no hardcoded constants.
-- Application reads these via IConfiguracionService + cache.
-- ------------------------------------------------------------
CREATE TABLE SystemConfigurations (
    Id              INT             NOT NULL    IDENTITY(1,1),
    ConfigKey       VARCHAR(100)    NOT NULL,
    ConfigValue     VARCHAR(500)    NOT NULL,
    DataType        VARCHAR(10)     NOT NULL,   -- 'Decimal' | 'Int' | 'Bool' | 'String'
    Description     NVARCHAR(500)   NOT NULL,
    Module          VARCHAR(20)     NOT NULL,   -- 'Points' | 'Redemption' | 'Fraud' | 'Bonus' | 'Auth' | 'General'
    IsEditable      BIT             NOT NULL    CONSTRAINT DF_SysCfg_IsEditable  DEFAULT 1,
    IsActive        BIT             NOT NULL    CONSTRAINT DF_SysCfg_IsActive     DEFAULT 1,
    MinValue        VARCHAR(50)     NULL,       -- inclusive lower bound for validation
    MaxValue        VARCHAR(50)     NULL,       -- inclusive upper bound for validation
    UpdatedAt       DATETIME2(7)    NOT NULL    CONSTRAINT DF_SysCfg_UpdatedAt    DEFAULT SYSUTCDATETIME(),
    UpdatedBy       NVARCHAR(100)   NOT NULL    CONSTRAINT DF_SysCfg_UpdatedBy    DEFAULT 'SYSTEM',

    CONSTRAINT PK_SystemConfigurations          PRIMARY KEY (Id),
    CONSTRAINT UQ_SystemConfigurations_Key      UNIQUE (ConfigKey),
    CONSTRAINT CK_SysCfg_DataType              CHECK (DataType IN ('Decimal','Int','Bool','String')),
    CONSTRAINT CK_SysCfg_Module                CHECK (Module   IN ('Points','Redemption','Fraud','Bonus','Auth','General'))
);
GO

-- Audit trail for every configuration change (who changed what and when)
CREATE TABLE SystemConfigurationAudit (
    Id          BIGINT          NOT NULL    IDENTITY(1,1),
    ConfigKey   VARCHAR(100)    NOT NULL,
    OldValue    VARCHAR(500)    NOT NULL,
    NewValue    VARCHAR(500)    NOT NULL,
    ChangedAt   DATETIME2(7)    NOT NULL    CONSTRAINT DF_SysCfgAudit_ChangedAt DEFAULT SYSUTCDATETIME(),
    ChangedBy   NVARCHAR(100)   NOT NULL,
    IpAddress   VARCHAR(45)     NULL,       -- supports IPv6

    CONSTRAINT PK_SystemConfigurationAudit PRIMARY KEY (Id)
);
GO


-- ============================================================
-- SECTION 2: OPERATIONAL TABLES
-- ============================================================

-- ------------------------------------------------------------
-- Branches  (Sucursales)
-- RowVersion: protects concurrent updates to branch data.
-- ------------------------------------------------------------
CREATE TABLE Branches (
    Id          UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_Branches_Id      DEFAULT NEWSEQUENTIALID(),
    CountryId   TINYINT             NOT NULL,
    Code        VARCHAR(10)         NOT NULL,   -- e.g. 'HN-01'
    Name        NVARCHAR(200)       NOT NULL,
    Address     NVARCHAR(500)       NOT NULL,
    Phone       VARCHAR(20)         NULL,
    IsActive    BIT                 NOT NULL    CONSTRAINT DF_Branches_IsActive DEFAULT 1,
    RowVersion  ROWVERSION          NOT NULL,
    CreatedAt   DATETIME2(7)        NOT NULL    CONSTRAINT DF_Branches_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2(7)        NOT NULL    CONSTRAINT DF_Branches_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Branches              PRIMARY KEY (Id),
    CONSTRAINT UQ_Branches_Code         UNIQUE (Code),
    CONSTRAINT FK_Branches_Countries    FOREIGN KEY (CountryId) REFERENCES Countries(Id)
);
GO

-- ------------------------------------------------------------
-- Users  (Operadores internos: Admin, Supervisor, Cashier)
-- Completely separate from Customers — different auth flow.
-- RowVersion: guards concurrent profile edits.
-- ------------------------------------------------------------
CREATE TABLE Users (
    Id                      UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_Users_Id      DEFAULT NEWSEQUENTIALID(),
    BranchId                UNIQUEIDENTIFIER    NULL,       -- NULL = Admin without fixed branch
    Username                VARCHAR(100)        NOT NULL,
    PasswordHash            VARCHAR(256)        NOT NULL,   -- BCrypt (cost ≥ 12)
    FullName                NVARCHAR(200)       NOT NULL,
    Email                   VARCHAR(200)        NULL,
    -- Role: 1=Admin | 2=Supervisor | 3=Cashier
    Role                    TINYINT             NOT NULL,
    -- Status: 1=Active | 2=Inactive | 3=Locked
    Status                  TINYINT             NOT NULL    CONSTRAINT DF_Users_Status  DEFAULT 1,
    FailedLoginAttempts     INT                 NOT NULL    CONSTRAINT DF_Users_FailedAttempts DEFAULT 0,
    LockedUntil             DATETIME2(7)        NULL,
    LastLoginAt             DATETIME2(7)        NULL,
    RowVersion              ROWVERSION          NOT NULL,
    CreatedAt               DATETIME2(7)        NOT NULL    CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt               DATETIME2(7)        NOT NULL    CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy               NVARCHAR(100)       NOT NULL    CONSTRAINT DF_Users_CreatedBy DEFAULT 'SYSTEM',

    CONSTRAINT PK_Users                     PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username            UNIQUE (Username),
    CONSTRAINT FK_Users_Branches            FOREIGN KEY (BranchId) REFERENCES Branches(Id),
    CONSTRAINT CK_Users_Role               CHECK (Role   IN (1, 2, 3)),
    CONSTRAINT CK_Users_Status             CHECK (Status IN (1, 2, 3)),
    CONSTRAINT CK_Users_FailedAttempts     CHECK (FailedLoginAttempts >= 0)
);
GO

-- ------------------------------------------------------------
-- Customers  (Clientes de la app móvil)
-- Auth factors: IdentityNumber (primary) + Phone (secondary).
-- RowVersion: guards concurrent profile updates.
-- ------------------------------------------------------------
CREATE TABLE Customers (
    Id                      UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_Customers_Id      DEFAULT NEWSEQUENTIALID(),
    IdentityNumber          VARCHAR(20)         NOT NULL,   -- DPI / Cédula / Pasaporte
    FirstName               NVARCHAR(100)       NOT NULL,
    LastName                NVARCHAR(100)       NOT NULL,
    Phone                   VARCHAR(20)         NOT NULL,   -- 2nd authentication factor
    -- Status: 1=Active | 2=Blocked (auto, antifraude) | 3=Suspended (manual, supervisor)
    Status                  TINYINT             NOT NULL    CONSTRAINT DF_Customers_Status  DEFAULT 1,
    FailedLoginAttempts     INT                 NOT NULL    CONSTRAINT DF_Customers_FailedAttempts DEFAULT 0,
    LockedUntil             DATETIME2(7)        NULL,
    LastLoginAt             DATETIME2(7)        NULL,
    DeviceToken             VARCHAR(500)        NULL,       -- FCM / APNS push token
    RowVersion              ROWVERSION          NOT NULL,
    CreatedAt               DATETIME2(7)        NOT NULL    CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt               DATETIME2(7)        NOT NULL    CONSTRAINT DF_Customers_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Customers                 PRIMARY KEY (Id),
    CONSTRAINT UQ_Customers_IdentityNumber  UNIQUE (IdentityNumber),
    CONSTRAINT UQ_Customers_Phone           UNIQUE (Phone),
    CONSTRAINT CK_Customers_Status         CHECK (Status IN (1, 2, 3)),
    CONSTRAINT CK_Customers_FailedAttempts CHECK (FailedLoginAttempts >= 0)
);
GO

-- ------------------------------------------------------------
-- CustomerProfiles  (Perfil extendido)
-- Separated from Customers so partial registration works.
-- Profile completion level drives the registration bonus.
-- ------------------------------------------------------------
CREATE TABLE CustomerProfiles (
    Id                      UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_CustProf_Id DEFAULT NEWSEQUENTIALID(),
    CustomerId              UNIQUEIDENTIFIER    NOT NULL,
    Email                   VARCHAR(200)        NULL,
    BirthDate               DATE                NULL,
    Address                 NVARCHAR(500)       NULL,
    CountryId               TINYINT             NULL,
    -- ProfileCompletionLevel: 1=PhoneOnly (L.0) | 2=WithEmail (L.5) | 3=Complete (L.15)
    ProfileCompletionLevel  TINYINT             NOT NULL    CONSTRAINT DF_CustProf_Level  DEFAULT 1,
    IsProfileComplete       BIT                 NOT NULL    CONSTRAINT DF_CustProf_Complete DEFAULT 0,
    -- BonusApplied tracks the L amount credited (0, 5 or 15) to prevent double-bonus
    BonusApplied            DECIMAL(18,2)       NOT NULL    CONSTRAINT DF_CustProf_Bonus  DEFAULT 0.00,
    UpdatedAt               DATETIME2(7)        NOT NULL    CONSTRAINT DF_CustProf_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_CustomerProfiles                  PRIMARY KEY (Id),
    CONSTRAINT UQ_CustomerProfiles_CustomerId       UNIQUE (CustomerId),
    CONSTRAINT FK_CustomerProfiles_Customers        FOREIGN KEY (CustomerId)  REFERENCES Customers(Id),
    CONSTRAINT FK_CustomerProfiles_Countries        FOREIGN KEY (CountryId)   REFERENCES Countries(Id),
    CONSTRAINT CK_CustProf_Level                   CHECK (ProfileCompletionLevel IN (1, 2, 3)),
    CONSTRAINT CK_CustProf_Bonus                   CHECK (BonusApplied >= 0)
);
GO

-- ------------------------------------------------------------
-- PointsAccounts  (Cuenta de puntos — 1:1 con Customer)
-- RowVersion is CRITICAL here: prevents lost-update race conditions
-- when two concurrent requests try to add/deduct points simultaneously.
-- EF Core uses this for optimistic concurrency via [Timestamp] attribute.
-- ------------------------------------------------------------
CREATE TABLE PointsAccounts (
    Id                  UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_PtAcct_Id DEFAULT NEWSEQUENTIALID(),
    CustomerId          UNIQUEIDENTIFIER    NOT NULL,
    Balance             DECIMAL(18,2)       NOT NULL    CONSTRAINT DF_PtAcct_Balance          DEFAULT 0.00,
    TotalAccumulated    DECIMAL(18,2)       NOT NULL    CONSTRAINT DF_PtAcct_TotalAccumulated  DEFAULT 0.00,
    TotalRedeemed       DECIMAL(18,2)       NOT NULL    CONSTRAINT DF_PtAcct_TotalRedeemed     DEFAULT 0.00,
    LastActivityAt      DATETIME2(7)        NULL,
    RowVersion          ROWVERSION          NOT NULL,   -- CRITICAL: optimistic concurrency lock
    CreatedAt           DATETIME2(7)        NOT NULL    CONSTRAINT DF_PtAcct_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2(7)        NOT NULL    CONSTRAINT DF_PtAcct_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PointsAccounts                PRIMARY KEY (Id),
    CONSTRAINT UQ_PointsAccounts_CustomerId     UNIQUE (CustomerId),
    CONSTRAINT FK_PointsAccounts_Customers      FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    CONSTRAINT CK_PtAcct_Balance               CHECK (Balance          >= 0),
    CONSTRAINT CK_PtAcct_TotalAccumulated      CHECK (TotalAccumulated >= 0),
    CONSTRAINT CK_PtAcct_TotalRedeemed         CHECK (TotalRedeemed    >= 0)
);
GO

-- ------------------------------------------------------------
-- Shipments  (Guías de envío)
-- Retrieved from Cargo Expreso central API at scan time.
-- Once created here they are the source of truth for validation.
-- ------------------------------------------------------------
CREATE TABLE Shipments (
    Id                      UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_Ship_Id DEFAULT NEWSEQUENTIALID(),
    ShipmentNumber          VARCHAR(50)         NOT NULL,   -- Número de guía (from CE central system)
    OwnerIdentityNumber     VARCHAR(20)         NOT NULL,   -- Identity on the guide — must match scanner
    ShipmentAmount          DECIMAL(18,2)       NOT NULL,
    -- Status: 1=Pending | 2=Scanned | 3=Expired
    Status                  TINYINT             NOT NULL    CONSTRAINT DF_Ship_Status DEFAULT 1,
    IssuedAt                DATETIME2(7)        NOT NULL,   -- Original issue date from CE
    ExpiresAt               DATETIME2(7)        NOT NULL,   -- IssuedAt + VENTANA_ESCANEO_HORAS
    ScannedAt               DATETIME2(7)        NULL,
    ScannedByCustomerId     UNIQUEIDENTIFIER    NULL,
    PointsAwarded           DECIMAL(18,2)       NOT NULL    CONSTRAINT DF_Ship_Points DEFAULT 0.00,
    SourceSystem            VARCHAR(50)         NOT NULL    CONSTRAINT DF_Ship_Source DEFAULT 'API_CENTRAL_CE',
    CreatedAt               DATETIME2(7)        NOT NULL    CONSTRAINT DF_Ship_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Shipments                     PRIMARY KEY (Id),
    CONSTRAINT UQ_Shipments_ShipmentNumber      UNIQUE (ShipmentNumber),
    CONSTRAINT FK_Shipments_Customers           FOREIGN KEY (ScannedByCustomerId) REFERENCES Customers(Id),
    CONSTRAINT CK_Ship_Status                  CHECK (Status IN (1, 2, 3)),
    CONSTRAINT CK_Ship_Amount                  CHECK (ShipmentAmount > 0),
    CONSTRAINT CK_Ship_Points                  CHECK (PointsAwarded  >= 0),
    CONSTRAINT CK_Ship_Expiry                  CHECK (ExpiresAt > IssuedAt)
);
GO

-- ------------------------------------------------------------
-- PointsTransactions  (Ledger inmutable de movimientos)
-- Never UPDATE or DELETE rows here — append only.
-- TransactionType:
--   1 = Accumulation (shipment scan)
--   2 = Redemption   (canje applied at branch)
--   3 = RegistrationBonus
--   4 = Expiration   (manual or scheduled)
--   5 = ManualAdjustment (supervisor override)
-- ------------------------------------------------------------
CREATE TABLE PointsTransactions (
    Id                      UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_PtTrx_Id DEFAULT NEWSEQUENTIALID(),
    PointsAccountId         UNIQUEIDENTIFIER    NOT NULL,
    CustomerId              UNIQUEIDENTIFIER    NOT NULL,
    TransactionType         TINYINT             NOT NULL,
    Amount                  DECIMAL(18,2)       NOT NULL,   -- positive = credit, negative = debit
    BalanceBefore           DECIMAL(18,2)       NOT NULL,
    BalanceAfter            DECIMAL(18,2)       NOT NULL,
    -- Contextual references (only one populated per row depending on type)
    ShipmentId              UNIQUEIDENTIFIER    NULL,
    RedemptionQrCodeId      UNIQUEIDENTIFIER    NULL,
    BranchId                UNIQUEIDENTIFIER    NULL,
    OperatorUserId          UNIQUEIDENTIFIER    NULL,       -- Cashier who applied redemption
    Notes                   NVARCHAR(500)       NULL,
    IpAddress               VARCHAR(45)         NULL,
    CreatedAt               DATETIME2(7)        NOT NULL    CONSTRAINT DF_PtTrx_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PointsTransactions                PRIMARY KEY (Id),
    CONSTRAINT FK_PtTrx_PointsAccounts              FOREIGN KEY (PointsAccountId)    REFERENCES PointsAccounts(Id),
    CONSTRAINT FK_PtTrx_Customers                   FOREIGN KEY (CustomerId)         REFERENCES Customers(Id),
    CONSTRAINT FK_PtTrx_Shipments                   FOREIGN KEY (ShipmentId)         REFERENCES Shipments(Id),
    CONSTRAINT FK_PtTrx_Branches                    FOREIGN KEY (BranchId)           REFERENCES Branches(Id),
    CONSTRAINT FK_PtTrx_Users                       FOREIGN KEY (OperatorUserId)     REFERENCES Users(Id),
    CONSTRAINT CK_PtTrx_Type                        CHECK (TransactionType IN (1,2,3,4,5)),
    CONSTRAINT CK_PtTrx_Amount                      CHECK (Amount    <> 0),
    CONSTRAINT CK_PtTrx_BalanceAfter                CHECK (BalanceAfter >= 0)
);
GO

-- ------------------------------------------------------------
-- RedemptionRequests  (Solicitudes de canje — lifecycle header)
-- Status: 1=Pending | 2=QrGenerated | 3=Applied | 4=Expired | 5=Cancelled
-- ------------------------------------------------------------
CREATE TABLE RedemptionRequests (
    Id                  UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_RedReq_Id DEFAULT NEWSEQUENTIALID(),
    CustomerId          UNIQUEIDENTIFIER    NOT NULL,
    RequestedAmount     DECIMAL(18,2)       NOT NULL,
    Status              TINYINT             NOT NULL    CONSTRAINT DF_RedReq_Status DEFAULT 1,
    RequestedAt         DATETIME2(7)        NOT NULL    CONSTRAINT DF_RedReq_RequestedAt DEFAULT SYSUTCDATETIME(),
    CompletedAt         DATETIME2(7)        NULL,
    BranchId            UNIQUEIDENTIFIER    NULL,
    OperatorUserId      UNIQUEIDENTIFIER    NULL,
    CancellationReason  NVARCHAR(300)       NULL,

    CONSTRAINT PK_RedemptionRequests                PRIMARY KEY (Id),
    CONSTRAINT FK_RedReq_Customers                  FOREIGN KEY (CustomerId)     REFERENCES Customers(Id),
    CONSTRAINT FK_RedReq_Branches                   FOREIGN KEY (BranchId)       REFERENCES Branches(Id),
    CONSTRAINT FK_RedReq_Users                      FOREIGN KEY (OperatorUserId) REFERENCES Users(Id),
    CONSTRAINT CK_RedReq_Status                     CHECK (Status IN (1,2,3,4,5)),
    CONSTRAINT CK_RedReq_Amount                     CHECK (RequestedAmount > 0)
);
GO

-- ------------------------------------------------------------
-- RedemptionQrCodes  (QR codes generados para canje en sucursal)
-- RowVersion: prevents double-use race condition when two cashier
-- terminals scan the same QR simultaneously.
-- QrCode column (NEWID) is the value encoded in the QR image —
-- uses NEWID (random) instead of NEWSEQUENTIALID to prevent guessing.
-- ------------------------------------------------------------
CREATE TABLE RedemptionQrCodes (
    Id                      UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_QR_Id     DEFAULT NEWSEQUENTIALID(),
    RedemptionRequestId     UNIQUEIDENTIFIER    NOT NULL,
    CustomerId              UNIQUEIDENTIFIER    NOT NULL,
    QrCode                  UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_QR_Code   DEFAULT NEWID(),    -- random, public-facing
    Amount                  DECIMAL(18,2)       NOT NULL,
    GeneratedAt             DATETIME2(7)        NOT NULL    CONSTRAINT DF_QR_GeneratedAt DEFAULT SYSUTCDATETIME(),
    ExpiresAt               DATETIME2(7)        NOT NULL,   -- GeneratedAt + VIGENCIA_QR_MINUTOS
    IsUsed                  BIT                 NOT NULL    CONSTRAINT DF_QR_IsUsed DEFAULT 0,
    UsedAt                  DATETIME2(7)        NULL,
    UsedByBranchId          UNIQUEIDENTIFIER    NULL,
    UsedByOperatorId        UNIQUEIDENTIFIER    NULL,
    RowVersion              ROWVERSION          NOT NULL,   -- prevents concurrent double-scan

    CONSTRAINT PK_RedemptionQrCodes                 PRIMARY KEY (Id),
    CONSTRAINT UQ_RedemptionQrCodes_QrCode          UNIQUE (QrCode),
    CONSTRAINT FK_QR_RedemptionRequests             FOREIGN KEY (RedemptionRequestId) REFERENCES RedemptionRequests(Id),
    CONSTRAINT FK_QR_Customers                      FOREIGN KEY (CustomerId)          REFERENCES Customers(Id),
    CONSTRAINT FK_QR_Branches                       FOREIGN KEY (UsedByBranchId)      REFERENCES Branches(Id),
    CONSTRAINT FK_QR_Users                          FOREIGN KEY (UsedByOperatorId)    REFERENCES Users(Id),
    CONSTRAINT CK_QR_Amount                         CHECK (Amount > 0),
    CONSTRAINT CK_QR_Expiry                         CHECK (ExpiresAt > GeneratedAt)
);
GO

-- ------------------------------------------------------------
-- RefreshTokens  (JWT Refresh Token store)
-- Stores hashed tokens for both Customers and Users.
-- Only one of CustomerId / UserId is populated per row (enforced by CK).
-- RevokedReason: 'Logout' | 'Fraud' | 'Expired' | 'Replaced'
-- ------------------------------------------------------------
CREATE TABLE RefreshTokens (
    Id              UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_RT_Id DEFAULT NEWSEQUENTIALID(),
    TokenHash       VARCHAR(256)        NOT NULL,   -- SHA-256 of the actual token (never store plaintext)
    CustomerId      UNIQUEIDENTIFIER    NULL,
    UserId          UNIQUEIDENTIFIER    NULL,
    ExpiresAt       DATETIME2(7)        NOT NULL,
    IsRevoked       BIT                 NOT NULL    CONSTRAINT DF_RT_IsRevoked  DEFAULT 0,
    RevokedAt       DATETIME2(7)        NULL,
    RevokedReason   VARCHAR(20)         NULL,
    CreatedAt       DATETIME2(7)        NOT NULL    CONSTRAINT DF_RT_CreatedAt  DEFAULT SYSUTCDATETIME(),
    IpAddress       VARCHAR(45)         NULL,
    DeviceInfo      NVARCHAR(200)       NULL,

    CONSTRAINT PK_RefreshTokens                 PRIMARY KEY (Id),
    CONSTRAINT UQ_RefreshTokens_TokenHash       UNIQUE (TokenHash),
    CONSTRAINT FK_RT_Customers                  FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    CONSTRAINT FK_RT_Users                      FOREIGN KEY (UserId)     REFERENCES Users(Id),
    -- Exactly one owner per token
    CONSTRAINT CK_RT_ExclusiveOwner            CHECK (
        (CustomerId IS NOT NULL AND UserId IS NULL) OR
        (CustomerId IS NULL     AND UserId IS NOT NULL)
    ),
    CONSTRAINT CK_RT_RevokedReason             CHECK (RevokedReason IN ('Logout','Fraud','Expired','Replaced') OR RevokedReason IS NULL)
);
GO


-- ============================================================
-- SECTION 3: AUDIT & SECURITY TABLES
-- ============================================================

-- ------------------------------------------------------------
-- AuditLogs  (Trazabilidad total del sistema — append only)
-- BIGINT PK for high-volume sequential inserts (better perf than GUID).
-- OldValues / NewValues store JSON snapshots for full traceability.
-- Result: 1=Success | 2=Rejected | 3=Error
-- ------------------------------------------------------------
CREATE TABLE AuditLogs (
    Id              BIGINT              NOT NULL    IDENTITY(1,1),
    EntityType      VARCHAR(50)         NOT NULL,   -- 'Customer' | 'Shipment' | 'Redemption' | 'Auth' | 'Config'
    EntityId        VARCHAR(50)         NULL,       -- PK of the affected entity (as string)
    OperationType   VARCHAR(50)         NOT NULL,   -- 'Login' | 'Scan' | 'Redemption' | 'ConfigChange' | 'Block' ...
    CustomerId      UNIQUEIDENTIFIER    NULL,
    UserId          UNIQUEIDENTIFIER    NULL,
    BranchId        UNIQUEIDENTIFIER    NULL,
    OldValues       NVARCHAR(MAX)       NULL,       -- JSON snapshot before change
    NewValues       NVARCHAR(MAX)       NULL,       -- JSON snapshot after change
    IpAddress       VARCHAR(45)         NULL,
    UserAgent       NVARCHAR(500)       NULL,
    Result          TINYINT             NOT NULL,
    RejectionReason NVARCHAR(500)       NULL,
    CreatedAt       DATETIME2(7)        NOT NULL    CONSTRAINT DF_AL_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AuditLogs        PRIMARY KEY (Id),
    CONSTRAINT CK_AL_Result        CHECK (Result IN (1, 2, 3))
);
GO

-- ------------------------------------------------------------
-- LoginAttempts  (Rate limiting & fraud detection per identity/IP)
-- BIGINT PK — high insert volume, append only.
-- UserType: 1=Customer | 2=Operator
-- ------------------------------------------------------------
CREATE TABLE LoginAttempts (
    Id              BIGINT          NOT NULL    IDENTITY(1,1),
    IdentityNumber  VARCHAR(20)     NULL,       -- NULL when identity is unknown
    AttemptedAt     DATETIME2(7)    NOT NULL    CONSTRAINT DF_LA_AttemptedAt DEFAULT SYSUTCDATETIME(),
    IsSuccessful    BIT             NOT NULL,
    IpAddress       VARCHAR(45)     NULL,
    DeviceInfo      NVARCHAR(200)   NULL,
    -- FailureReason: 'NotFound' | 'WrongPhone' | 'AccountLocked' | 'AccountSuspended' | 'RateLimited'
    FailureReason   VARCHAR(30)     NULL,
    UserType        TINYINT         NOT NULL    CONSTRAINT DF_LA_UserType DEFAULT 1,

    CONSTRAINT PK_LoginAttempts    PRIMARY KEY (Id),
    CONSTRAINT CK_LA_UserType      CHECK (UserType IN (1, 2))
);
GO

-- ------------------------------------------------------------
-- FraudAlerts  (Motor antifraude — alertas automáticas y manuales)
-- Severity: 1=Low | 2=Medium | 3=High | 4=Critical
-- Status:   1=Open | 2=UnderReview | 3=Resolved | 4=Dismissed
-- AlertType examples:
--   'REPEATED_FAILED_SCAN' | 'IDENTITY_MISMATCH' | 'QR_REUSE_ATTEMPT'
--   'SCAN_VELOCITY' | 'MULTIPLE_DEVICES' | 'ACCOUNT_BLOCKED_AUTO'
-- ------------------------------------------------------------
CREATE TABLE FraudAlerts (
    Id                  UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT DF_FA_Id DEFAULT NEWSEQUENTIALID(),
    CustomerId          UNIQUEIDENTIFIER    NULL,
    AlertType           VARCHAR(50)         NOT NULL,
    Severity            TINYINT             NOT NULL,
    Description         NVARCHAR(1000)      NOT NULL,
    Status              TINYINT             NOT NULL    CONSTRAINT DF_FA_Status DEFAULT 1,
    RelatedEntityType   VARCHAR(50)         NULL,
    RelatedEntityId     VARCHAR(50)         NULL,
    DetectedAt          DATETIME2(7)        NOT NULL    CONSTRAINT DF_FA_DetectedAt  DEFAULT SYSUTCDATETIME(),
    ResolvedAt          DATETIME2(7)        NULL,
    ResolvedByUserId    UNIQUEIDENTIFIER    NULL,
    ResolutionNotes     NVARCHAR(500)       NULL,

    CONSTRAINT PK_FraudAlerts               PRIMARY KEY (Id),
    CONSTRAINT FK_FA_Customers              FOREIGN KEY (CustomerId)        REFERENCES Customers(Id),
    CONSTRAINT FK_FA_Users                  FOREIGN KEY (ResolvedByUserId)  REFERENCES Users(Id),
    CONSTRAINT CK_FA_Severity              CHECK (Severity IN (1, 2, 3, 4)),
    CONSTRAINT CK_FA_Status                CHECK (Status   IN (1, 2, 3, 4))
);
GO


-- ============================================================
-- SECTION 4: INDEXES
-- Tuned for the most frequent read patterns in production.
-- ============================================================

-- Customers
CREATE INDEX IX_Customers_Status
    ON Customers(Status)
    WHERE Status <> 1;                          -- only non-active rows (fraud monitoring)

-- Shipments
CREATE INDEX IX_Shipments_OwnerIdentity
    ON Shipments(OwnerIdentityNumber);
CREATE INDEX IX_Shipments_Status
    ON Shipments(Status);
CREATE INDEX IX_Shipments_ExpiresAt
    ON Shipments(ExpiresAt)
    WHERE Status = 1;                           -- only Pending shipments (expiry job)

-- PointsTransactions (high read volume: history, reports)
CREATE INDEX IX_PtTrx_Customer_Date
    ON PointsTransactions(CustomerId, CreatedAt DESC);
CREATE INDEX IX_PtTrx_AccountId
    ON PointsTransactions(PointsAccountId);
CREATE INDEX IX_PtTrx_Type
    ON PointsTransactions(TransactionType);

-- RedemptionRequests
CREATE INDEX IX_RedReq_CustomerId
    ON RedemptionRequests(CustomerId);
CREATE INDEX IX_RedReq_Status
    ON RedemptionRequests(Status);

-- RedemptionQrCodes
CREATE INDEX IX_QR_CustomerId
    ON RedemptionQrCodes(CustomerId);
CREATE INDEX IX_QR_ExpiresAt
    ON RedemptionQrCodes(ExpiresAt)
    WHERE IsUsed = 0;                           -- only active QR codes (expiry job)

-- RefreshTokens
CREATE INDEX IX_RT_CustomerId
    ON RefreshTokens(CustomerId)
    WHERE CustomerId IS NOT NULL;
CREATE INDEX IX_RT_UserId
    ON RefreshTokens(UserId)
    WHERE UserId IS NOT NULL;
CREATE INDEX IX_RT_ExpiresAt
    ON RefreshTokens(ExpiresAt)
    WHERE IsRevoked = 0;                        -- only valid tokens (cleanup job)

-- AuditLogs (minimize indexes — table is append-heavy)
CREATE INDEX IX_AL_Customer_Date
    ON AuditLogs(CustomerId, CreatedAt DESC)
    WHERE CustomerId IS NOT NULL;
CREATE INDEX IX_AL_Entity
    ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AL_CreatedAt
    ON AuditLogs(CreatedAt DESC);

-- LoginAttempts
CREATE INDEX IX_LA_Identity_Date
    ON LoginAttempts(IdentityNumber, AttemptedAt DESC)
    WHERE IdentityNumber IS NOT NULL;
CREATE INDEX IX_LA_IP_Date
    ON LoginAttempts(IpAddress, AttemptedAt DESC)
    WHERE IpAddress IS NOT NULL;

-- FraudAlerts
CREATE INDEX IX_FA_CustomerId
    ON FraudAlerts(CustomerId)
    WHERE CustomerId IS NOT NULL;
CREATE INDEX IX_FA_OpenStatus
    ON FraudAlerts(Status, DetectedAt DESC)
    WHERE Status IN (1, 2);                     -- only Open / UnderReview

-- Users
CREATE INDEX IX_Users_BranchId
    ON Users(BranchId)
    WHERE BranchId IS NOT NULL;

-- SystemConfigurationAudit
CREATE INDEX IX_SysCfgAudit_Key_Date
    ON SystemConfigurationAudit(ConfigKey, ChangedAt DESC);
GO


-- ============================================================
-- SECTION 5: STORED PROCEDURES
-- ============================================================

-- ------------------------------------------------------------
-- sp_ScanShipment
-- Validates and credits points for a shipment QR scan.
-- All 4 business rules enforced atomically.
-- Configuration read from SystemConfigurations at runtime.
-- ------------------------------------------------------------
GO
CREATE PROCEDURE sp_ScanShipment
    @ShipmentNumber     VARCHAR(50),
    @CustomerIdentity   VARCHAR(20),
    @IpAddress          VARCHAR(45)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @CustomerId         UNIQUEIDENTIFIER,
        @ShipmentId         UNIQUEIDENTIFIER,
        @AccountId          UNIQUEIDENTIFIER,
        @ShipmentAmount     DECIMAL(18,2),
        @AccumulationRate   DECIMAL(18,4),
        @PointsToAward      DECIMAL(18,2),
        @BalanceBefore      DECIMAL(18,2),
        @BalanceAfter       DECIMAL(18,2),
        @ExpiresAt          DATETIME2(7),
        @ShipmentStatus     TINYINT;

    -- Load configurable accumulation rate
    SELECT @AccumulationRate = TRY_CAST(ConfigValue AS DECIMAL(18,4)) / 100
    FROM SystemConfigurations
    WHERE ConfigKey = 'TASA_ACUMULACION_PUNTOS' AND IsActive = 1;

    IF @AccumulationRate IS NULL
    BEGIN
        SELECT 'ERROR' AS Result, 'Configuration TASA_ACUMULACION_PUNTOS not found' AS Message;
        RETURN;
    END

    -- Retrieve shipment data
    SELECT
        @ShipmentId     = Id,
        @ShipmentAmount = ShipmentAmount,
        @ExpiresAt      = ExpiresAt,
        @ShipmentStatus = Status
    FROM Shipments
    WHERE ShipmentNumber = @ShipmentNumber;

    -- Rule 1: Shipment must exist
    IF @ShipmentId IS NULL
    BEGIN
        INSERT INTO AuditLogs (EntityType, EntityId, OperationType, IpAddress, Result, RejectionReason)
        VALUES ('Shipment', @ShipmentNumber, 'Scan', @IpAddress, 2, 'ShipmentNotFound');

        SELECT 'ERROR' AS Result, 'Guía no encontrada en el sistema' AS Message;
        RETURN;
    END

    -- Rule 2: Scan window must be open (72h)
    IF SYSUTCDATETIME() > @ExpiresAt
    BEGIN
        UPDATE Shipments SET Status = 3 WHERE Id = @ShipmentId AND Status = 1;

        INSERT INTO AuditLogs (EntityType, EntityId, OperationType, IpAddress, Result, RejectionReason)
        VALUES ('Shipment', CAST(@ShipmentId AS VARCHAR(50)), 'Scan', @IpAddress, 2, 'ScanWindowExpired');

        SELECT 'ERROR' AS Result, 'La ventana de escaneo de 72 horas ha expirado' AS Message;
        RETURN;
    END

    -- Rule 3: Shipment can only be scanned once
    IF @ShipmentStatus = 2
    BEGIN
        INSERT INTO AuditLogs (EntityType, EntityId, OperationType, IpAddress, Result, RejectionReason)
        VALUES ('Shipment', CAST(@ShipmentId AS VARCHAR(50)), 'Scan', @IpAddress, 2, 'AlreadyScanned');

        SELECT 'ERROR' AS Result, 'Esta guía ya fue escaneada anteriormente' AS Message;
        RETURN;
    END

    -- Rule 4: Customer identity must match the shipment
    IF NOT EXISTS (SELECT 1 FROM Shipments WHERE Id = @ShipmentId AND OwnerIdentityNumber = @CustomerIdentity)
    BEGIN
        INSERT INTO AuditLogs (EntityType, EntityId, OperationType, IpAddress, Result, RejectionReason)
        VALUES ('Shipment', CAST(@ShipmentId AS VARCHAR(50)), 'Scan', @IpAddress, 2, 'IdentityMismatch');

        SELECT 'ERROR' AS Result, 'El número de identidad no coincide con la guía' AS Message;
        RETURN;
    END

    -- Get customer and account
    SELECT @CustomerId = Id FROM Customers WHERE IdentityNumber = @CustomerIdentity AND Status = 1;

    IF @CustomerId IS NULL
    BEGIN
        SELECT 'ERROR' AS Result, 'Cliente no encontrado o inactivo' AS Message;
        RETURN;
    END

    SELECT @AccountId = Id, @BalanceBefore = Balance
    FROM PointsAccounts WHERE CustomerId = @CustomerId;

    -- Calculate points
    SET @PointsToAward = ROUND(@ShipmentAmount * @AccumulationRate, 2);
    SET @BalanceAfter  = @BalanceBefore + @PointsToAward;

    BEGIN TRANSACTION;

    -- Mark shipment as scanned
    UPDATE Shipments
    SET Status = 2, ScannedAt = SYSUTCDATETIME(), ScannedByCustomerId = @CustomerId,
        PointsAwarded = @PointsToAward
    WHERE Id = @ShipmentId AND Status = 1;   -- status check prevents race condition

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK;
        SELECT 'ERROR' AS Result, 'Conflicto de concurrencia al escanear la guía' AS Message;
        RETURN;
    END

    -- Update balance
    UPDATE PointsAccounts
    SET Balance          = @BalanceAfter,
        TotalAccumulated = TotalAccumulated + @PointsToAward,
        LastActivityAt   = SYSUTCDATETIME(),
        UpdatedAt        = SYSUTCDATETIME()
    WHERE Id = @AccountId;

    -- Record transaction
    INSERT INTO PointsTransactions
        (PointsAccountId, CustomerId, TransactionType, Amount, BalanceBefore, BalanceAfter,
         ShipmentId, IpAddress)
    VALUES
        (@AccountId, @CustomerId, 1, @PointsToAward, @BalanceBefore, @BalanceAfter,
         @ShipmentId, @IpAddress);

    -- Audit
    INSERT INTO AuditLogs (EntityType, EntityId, OperationType, CustomerId, IpAddress, Result)
    VALUES ('Shipment', CAST(@ShipmentId AS VARCHAR(50)), 'Scan', @CustomerId, @IpAddress, 1);

    COMMIT;

    SELECT
        'OK'              AS Result,
        'Puntos acreditados correctamente' AS Message,
        @PointsToAward    AS PointsAwarded,
        @BalanceAfter     AS NewBalance;
END
GO

-- ------------------------------------------------------------
-- sp_ApplyRedemption
-- Validates and applies a QR redemption at a branch.
-- RowVersion check prevents double-use race condition.
-- ------------------------------------------------------------
GO
CREATE PROCEDURE sp_ApplyRedemption
    @QrCode         UNIQUEIDENTIFIER,
    @BranchId       UNIQUEIDENTIFIER,
    @OperatorUserId UNIQUEIDENTIFIER,
    @IpAddress      VARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @QrId           UNIQUEIDENTIFIER,
        @RequestId      UNIQUEIDENTIFIER,
        @CustomerId     UNIQUEIDENTIFIER,
        @AccountId      UNIQUEIDENTIFIER,
        @Amount         DECIMAL(18,2),
        @ExpiresAt      DATETIME2(7),
        @BalanceBefore  DECIMAL(18,2),
        @BalanceAfter   DECIMAL(18,2),
        @QrRowVersion   BINARY(8),
        @MinBalance     DECIMAL(18,2);

    -- Load minimum balance configuration
    SELECT @MinBalance = TRY_CAST(ConfigValue AS DECIMAL(18,2))
    FROM SystemConfigurations
    WHERE ConfigKey = 'SALDO_MINIMO_CANJE' AND IsActive = 1;

    -- Retrieve QR details
    SELECT
        @QrId        = Id,
        @RequestId   = RedemptionRequestId,
        @CustomerId  = CustomerId,
        @Amount      = Amount,
        @ExpiresAt   = ExpiresAt,
        @QrRowVersion = RowVersion
    FROM RedemptionQrCodes
    WHERE QrCode = @QrCode AND IsUsed = 0;

    IF @QrId IS NULL
    BEGIN
        INSERT INTO AuditLogs (EntityType, EntityId, OperationType, BranchId, UserId, IpAddress, Result, RejectionReason)
        VALUES ('QrCode', CAST(@QrCode AS VARCHAR(50)), 'Redemption', @BranchId, @OperatorUserId, @IpAddress, 2, 'QrInvalidOrUsed');

        SELECT 'ERROR' AS Result, 'QR inválido o ya utilizado' AS Message;
        RETURN;
    END

    -- Check expiry
    IF SYSUTCDATETIME() > @ExpiresAt
    BEGIN
        UPDATE RedemptionQrCodes SET IsUsed = 1, UsedAt = SYSUTCDATETIME() WHERE Id = @QrId;
        UPDATE RedemptionRequests SET Status = 4, CompletedAt = SYSUTCDATETIME() WHERE Id = @RequestId;

        INSERT INTO AuditLogs (EntityType, EntityId, OperationType, CustomerId, BranchId, IpAddress, Result, RejectionReason)
        VALUES ('QrCode', CAST(@QrId AS VARCHAR(50)), 'Redemption', @CustomerId, @BranchId, @IpAddress, 2, 'QrExpired');

        SELECT 'ERROR' AS Result, 'El QR ha expirado (vigencia de 30 minutos superada)' AS Message;
        RETURN;
    END

    -- Get account and current balance
    SELECT @AccountId = Id, @BalanceBefore = Balance
    FROM PointsAccounts WHERE CustomerId = @CustomerId;

    IF @BalanceBefore < @MinBalance OR @BalanceBefore < @Amount
    BEGIN
        SELECT 'ERROR' AS Result, 'Saldo insuficiente para completar el canje' AS Message;
        RETURN;
    END

    SET @BalanceAfter = @BalanceBefore - @Amount;

    BEGIN TRANSACTION;

    -- Mark QR as used — RowVersion ensures only one concurrent caller succeeds
    UPDATE RedemptionQrCodes
    SET IsUsed = 1, UsedAt = SYSUTCDATETIME(),
        UsedByBranchId = @BranchId, UsedByOperatorId = @OperatorUserId
    WHERE Id = @QrId AND RowVersion = @QrRowVersion;   -- optimistic concurrency guard

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK;
        SELECT 'ERROR' AS Result, 'Conflicto de concurrencia: el QR ya fue procesado' AS Message;
        RETURN;
    END

    -- Deduct balance
    UPDATE PointsAccounts
    SET Balance        = @BalanceAfter,
        TotalRedeemed  = TotalRedeemed + @Amount,
        LastActivityAt = SYSUTCDATETIME(),
        UpdatedAt      = SYSUTCDATETIME()
    WHERE Id = @AccountId;

    -- Update request status
    UPDATE RedemptionRequests
    SET Status = 3, CompletedAt = SYSUTCDATETIME(),
        BranchId = @BranchId, OperatorUserId = @OperatorUserId
    WHERE Id = @RequestId;

    -- Record transaction
    DECLARE @TrxId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO PointsTransactions
        (Id, PointsAccountId, CustomerId, TransactionType, Amount, BalanceBefore, BalanceAfter,
         RedemptionQrCodeId, BranchId, OperatorUserId, IpAddress)
    VALUES
        (@TrxId, @AccountId, @CustomerId, 2, -@Amount, @BalanceBefore, @BalanceAfter,
         @QrId, @BranchId, @OperatorUserId, @IpAddress);

    -- Audit
    INSERT INTO AuditLogs (EntityType, EntityId, OperationType, CustomerId, UserId, BranchId, IpAddress, Result)
    VALUES ('QrCode', CAST(@QrId AS VARCHAR(50)), 'Redemption', @CustomerId, @OperatorUserId, @BranchId, @IpAddress, 1);

    COMMIT;

    SELECT
        'OK'            AS Result,
        'Canje aplicado correctamente' AS Message,
        @Amount         AS AmountRedeemed,
        @BalanceAfter   AS RemainingBalance;
END
GO

-- ------------------------------------------------------------
-- sp_ApplyRegistrationBonus
-- Applies bonus points based on profile completion level.
-- Reads bonus amounts from SystemConfigurations at runtime.
-- ------------------------------------------------------------
GO
CREATE PROCEDURE sp_ApplyRegistrationBonus
    @CustomerId         UNIQUEIDENTIFIER,
    @CompletionLevel    TINYINT   -- 1=PhoneOnly | 2=WithEmail | 3=Complete
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @BonusAmount    DECIMAL(18,2),
        @ConfigKey      VARCHAR(100),
        @AccountId      UNIQUEIDENTIFIER,
        @BalanceBefore  DECIMAL(18,2),
        @BalanceAfter   DECIMAL(18,2);

    SET @ConfigKey = CASE @CompletionLevel
        WHEN 1 THEN 'BONUS_SOLO_TELEFONO'
        WHEN 2 THEN 'BONUS_CON_CORREO'
        WHEN 3 THEN 'BONUS_PERFIL_COMPLETO'
        ELSE NULL
    END;

    IF @ConfigKey IS NULL
    BEGIN
        SELECT 'ERROR' AS Result, 'Nivel de completitud inválido' AS Message;
        RETURN;
    END

    SELECT @BonusAmount = TRY_CAST(ConfigValue AS DECIMAL(18,2))
    FROM SystemConfigurations
    WHERE ConfigKey = @ConfigKey AND IsActive = 1;

    -- No bonus for phone-only (L.0) — still record for audit
    IF @BonusAmount = 0 OR @BonusAmount IS NULL
    BEGIN
        SELECT 'OK' AS Result, 'Sin bonus para este nivel de perfil' AS Message, 0 AS BonusApplied;
        RETURN;
    END

    SELECT @AccountId = Id, @BalanceBefore = Balance
    FROM PointsAccounts WHERE CustomerId = @CustomerId;

    SET @BalanceAfter = @BalanceBefore + @BonusAmount;

    BEGIN TRANSACTION;

    UPDATE PointsAccounts
    SET Balance          = @BalanceAfter,
        TotalAccumulated = TotalAccumulated + @BonusAmount,
        LastActivityAt   = SYSUTCDATETIME(),
        UpdatedAt        = SYSUTCDATETIME()
    WHERE Id = @AccountId;

    UPDATE CustomerProfiles
    SET BonusApplied          = @BonusAmount,
        ProfileCompletionLevel = @CompletionLevel,
        IsProfileComplete      = CASE WHEN @CompletionLevel = 3 THEN 1 ELSE 0 END,
        UpdatedAt              = SYSUTCDATETIME()
    WHERE CustomerId = @CustomerId;

    INSERT INTO PointsTransactions
        (PointsAccountId, CustomerId, TransactionType, Amount, BalanceBefore, BalanceAfter, Notes)
    VALUES
        (@AccountId, @CustomerId, 3, @BonusAmount, @BalanceBefore, @BalanceAfter,
         'Bonus por registro — nivel ' + CAST(@CompletionLevel AS VARCHAR(1)));

    COMMIT;

    SELECT
        'OK'            AS Result,
        'Bonus aplicado' AS Message,
        @BonusAmount    AS BonusApplied,
        @BalanceAfter   AS NewBalance;
END
GO

-- ------------------------------------------------------------
-- sp_ExpireShipments
-- Marks Pending shipments past their expiry as Expired.
-- Designed to run as a scheduled job (e.g. every hour).
-- ------------------------------------------------------------
GO
CREATE PROCEDURE sp_ExpireShipments
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Shipments
    SET Status = 3
    WHERE Status = 1
      AND ExpiresAt < SYSUTCDATETIME();

    SELECT @@ROWCOUNT AS ShipmentsExpired;
END
GO

-- ------------------------------------------------------------
-- sp_ExpireQrCodes
-- Marks unused, expired QR codes and their parent requests.
-- Designed to run as a scheduled job (e.g. every 5 minutes).
-- ------------------------------------------------------------
GO
CREATE PROCEDURE sp_ExpireQrCodes
AS
BEGIN
    SET NOCOUNT ON;

    -- Expire QR codes
    UPDATE RedemptionQrCodes
    SET IsUsed = 1, UsedAt = SYSUTCDATETIME()
    WHERE IsUsed = 0
      AND ExpiresAt < SYSUTCDATETIME();

    -- Update parent requests to Expired
    UPDATE RedemptionRequests
    SET Status = 4, CompletedAt = SYSUTCDATETIME()
    WHERE Status = 2
      AND Id IN (
          SELECT RedemptionRequestId
          FROM RedemptionQrCodes
          WHERE IsUsed = 1 AND UsedAt IS NOT NULL AND UsedByBranchId IS NULL
      );

    SELECT @@ROWCOUNT AS QrCodesExpired;
END
GO


-- ============================================================
-- SECTION 6: SEED DATA
-- ============================================================

-- Countries
INSERT INTO Countries (Id, Code, Name, Currency, TimeZone) VALUES
(1, 'GT', 'Guatemala',   'GTQ', 'America/Guatemala'),
(2, 'SV', 'El Salvador', 'USD', 'America/El_Salvador'),
(3, 'HN', 'Honduras',    'HNL', 'America/Tegucigalpa'),
(4, 'CR', 'Costa Rica',  'CRC', 'America/Costa_Rica');
GO

-- System Configurations (all business rules — fully configurable)
INSERT INTO SystemConfigurations (ConfigKey, ConfigValue, DataType, Description, Module, IsEditable, MinValue, MaxValue) VALUES

-- Points
('TASA_ACUMULACION_PUNTOS',    '5',   'Decimal', 'Porcentaje del monto del envío acreditado como puntos (ej. 5 = 5%)',         'Points',      1, '1',   '20'),
('VENTANA_ESCANEO_HORAS',      '72',  'Int',     'Horas desde emisión de guía dentro de las cuales puede escanearse',          'Points',      1, '1',   '168'),

-- Redemption
('SALDO_MINIMO_CANJE',         '200', 'Decimal', 'Saldo mínimo en moneda local requerido para iniciar un canje',              'Redemption',  1, '50',  '1000'),
('VIGENCIA_QR_MINUTOS',        '30',  'Int',     'Minutos de vigencia del QR generado para canje en sucursal',               'Redemption',  1, '5',   '120'),

-- Registration bonus
('BONUS_SOLO_TELEFONO',        '0',   'Decimal', 'Bonificación al registrar solo con teléfono',                              'Bonus',       1, '0',   '50'),
('BONUS_CON_CORREO',           '5',   'Decimal', 'Bonificación al registrar correo electrónico adicional al teléfono',       'Bonus',       1, '0',   '50'),
('BONUS_PERFIL_COMPLETO',      '15',  'Decimal', 'Bonificación al completar todos los datos del perfil',                    'Bonus',       1, '0',   '100'),

-- Fraud / security (all configurable by admin)
('MAX_INTENTOS_LOGIN',         '5',   'Int',     'Intentos fallidos de login antes de bloqueo temporal',                    'Fraud',       1, '3',   '10'),
('TIEMPO_BLOQUEO_MINUTOS',     '30',  'Int',     'Minutos de bloqueo tras superar el límite de intentos fallidos',          'Fraud',       1, '5',   '1440'),
('MAX_ESCANEOS_POR_HORA',      '3',   'Int',     'Máximo de escaneos de guía permitidos por cliente por hora',              'Fraud',       1, '1',   '10'),
('MAX_QR_ACTIVOS_SIMULTANEOS', '2',   'Int',     'Máximo de QR de canje activos y no expirados por cliente a la vez',       'Fraud',       1, '1',   '5'),
('MAX_INTENTOS_FALLIDOS_SCAN', '3',   'Int',     'Intentos fallidos de escaneo consecutivos antes de generar alerta',       'Fraud',       1, '1',   '10'),

-- Auth (non-editable in admin UI — only via deployment config)
('ACCESS_TOKEN_MINUTOS',       '15',  'Int',     'Duración del JWT access token en minutos',                                'Auth',        0, '5',   '60'),
('REFRESH_TOKEN_DIAS',         '7',   'Int',     'Duración del refresh token en días',                                      'Auth',        0, '1',   '30');
GO

-- Branches (initial set — one per country HQ)
INSERT INTO Branches (Id, CountryId, Code, Name, Address) VALUES
('11111111-1111-1111-1111-111111111101', 3, 'HN-01', 'Sucursal Tegucigalpa Centro',    'Blvd. Morazán, Tegucigalpa, Honduras'),
('11111111-1111-1111-1111-111111111102', 3, 'HN-02', 'Sucursal San Pedro Sula',        'Col. Los Andes, San Pedro Sula, Honduras'),
('11111111-1111-1111-1111-111111111201', 1, 'GT-01', 'Sucursal Guatemala Central',     'Zona 10, Ciudad de Guatemala'),
('11111111-1111-1111-1111-111111111202', 1, 'GT-02', 'Sucursal Guatemala Zona 18',     'Zona 18, Ciudad de Guatemala'),
('11111111-1111-1111-1111-111111111301', 2, 'SV-01', 'Sucursal San Salvador',          'Col. Escalón, San Salvador, El Salvador'),
('11111111-1111-1111-1111-111111111401', 4, 'CR-01', 'Sucursal San José',              'Escazú, San José, Costa Rica');
GO

-- Default system admin user
-- Password 'Admin@2025!' — MUST be replaced with real BCrypt hash before deployment
INSERT INTO Users (Id, BranchId, Username, PasswordHash, FullName, Email, Role, Status, CreatedBy)
VALUES (
    'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA',
    NULL,
    'admin.sistema',
    '$2a$12$CHANGEME_REPLACE_WITH_REAL_BCRYPT_HASH_BEFORE_DEPLOY',
    'Administrador del Sistema',
    'admin@cargoexpreso.com',
    1,  -- Admin
    1,  -- Active
    'SYSTEM'
);
GO


-- ============================================================
-- SECTION 7: VERIFICATION QUERIES
-- Run after execution to confirm structure is correct.
-- ============================================================

-- Table row counts (should all be > 0 for seeded tables)
SELECT
    t.name                              AS TableName,
    p.rows                              AS RowCount
FROM sys.tables t
JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
ORDER BY t.name;
GO

-- Foreign keys summary
SELECT
    fk.name                             AS ForeignKey,
    tp.name                             AS ParentTable,
    tr.name                             AS ReferencedTable
FROM sys.foreign_keys fk
JOIN sys.tables tp ON fk.parent_object_id   = tp.object_id
JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
ORDER BY tp.name, fk.name;
GO

-- Configuration values loaded
SELECT ConfigKey, ConfigValue, Module, DataType, IsEditable
FROM SystemConfigurations
ORDER BY Module, ConfigKey;
GO

PRINT 'CargoExpresoPuntos schema v2.0.0 applied successfully.';
GO
