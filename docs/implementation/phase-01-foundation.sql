IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101195840_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251101195840_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [AvatarUrl] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE TABLE [RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101200718_InitialIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251101200718_InitialIdentity', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101203958_AddProducts'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Stock] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101203958_AddProducts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251101203958_AddProducts', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101210010_AddCartItems'
)
BEGIN
    CREATE TABLE [CartItems] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101210010_AddCartItems'
)
BEGIN
    CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101210010_AddCartItems'
)
BEGIN
    CREATE INDEX [IX_CartItems_UserId_ProductId] ON [CartItems] ([UserId], [ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101210010_AddCartItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251101210010_AddCartItems', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(50) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [ShippingAddress] nvarchar(500) NULL,
        [ShippingCity] nvarchar(100) NULL,
        [ShippingPostalCode] nvarchar(50) NULL,
        [ShippingCountry] nvarchar(100) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL DEFAULT 1,
        [Price] decimal(18,2) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE INDEX [IX_Orders_OrderDate] ON [Orders] ([OrderDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102095431_AddOrdersAndOrderItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251102095431_AddOrdersAndOrderItems', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102101324_AddWishlistItems'
)
BEGIN
    CREATE TABLE [WishlistItems] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ProductId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WishlistItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102101324_AddWishlistItems'
)
BEGIN
    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102101324_AddWishlistItems'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WishlistItems_UserId_ProductId] ON [WishlistItems] ([UserId], [ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102101324_AddWishlistItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251102101324_AddWishlistItems', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102102538_AddReviews'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102102538_AddReviews'
)
BEGIN
    CREATE INDEX [IX_Reviews_CreatedAt] ON [Reviews] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102102538_AddReviews'
)
BEGIN
    CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102102538_AddReviews'
)
BEGIN
    CREATE INDEX [IX_Reviews_Rating] ON [Reviews] ([Rating]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102102538_AddReviews'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reviews_UserId_ProductId] ON [Reviews] ([UserId], [ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102102538_AddReviews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251102102538_AddReviews', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [Products] ADD [BasePrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [Products] ADD [BaseSku] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [Products] ADD [CategoryId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [Products] ADD [IsFeatured] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [Products] ADD [ShortDescription] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [Products] ADD [Slug] nvarchar(220) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ProductId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [OrderItems] ALTER COLUMN [ProductId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ConfigurationSnapshotJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [CustomDesignId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ImageSnapshot] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ItemType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ProductNameSnapshot] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ProductVariantId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [SkuSnapshot] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    UPDATE Products
    SET BasePrice = Price
    WHERE BasePrice = 0;

    UPDATE Products
    SET Slug = CONCAT('product-', Id)
    WHERE Slug = '';

    UPDATE oi
    SET
        UnitPrice = oi.Price,
        ProductNameSnapshot = COALESCE(NULLIF(p.Name, ''), CONCAT('Product ', oi.ProductId)),
        SkuSnapshot = p.BaseSku,
        ImageSnapshot = NULLIF(p.ImageUrl, '')
    FROM OrderItems oi
    INNER JOIN Products p ON p.Id = oi.ProductId
    WHERE oi.ProductId IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [Addresses] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Label] nvarchar(80) NOT NULL,
        [RecipientName] nvarchar(120) NOT NULL,
        [Phone] nvarchar(40) NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(200) NULL,
        [City] nvarchar(100) NOT NULL,
        [DistrictOrProvince] nvarchar(100) NULL,
        [PostalCode] nvarchar(30) NULL,
        [Country] nvarchar(80) NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Addresses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] nvarchar(450) NULL,
        [Action] nvarchar(120) NOT NULL,
        [EntityType] nvarchar(120) NOT NULL,
        [EntityId] nvarchar(120) NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [CorrelationId] nvarchar(100) NULL,
        [IpAddress] nvarchar(64) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [Collections] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Slug] nvarchar(140) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [SortOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Collections] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [OrderStatusHistories] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [PreviousStatus] nvarchar(40) NULL,
        [NewStatus] nvarchar(40) NOT NULL,
        [ChangedByUserId] nvarchar(450) NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderStatusHistories_Users_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [Provider] nvarchar(80) NOT NULL,
        [Method] nvarchar(80) NOT NULL,
        [Status] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [ProviderTransactionId] nvarchar(160) NULL,
        [ProviderReference] nvarchar(160) NULL,
        [FailureReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [PaidAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [ProductMedia] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [Url] nvarchar(500) NULL,
        [MediaType] nvarchar(50) NOT NULL,
        [AltText] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductMedia] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductMedia_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [ProductVariants] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Sku] nvarchar(64) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [PriceOverride] decimal(18,2) NULL,
        [StockQuantity] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    INSERT INTO ProductMedia (ProductId, StorageKey, Url, MediaType, AltText, SortOrder, IsPrimary, CreatedAt)
    SELECT Id, ImageUrl, ImageUrl, 'image', Name, 0, CAST(1 AS bit), CreatedAt
    FROM Products
    WHERE NULLIF(ImageUrl, '') IS NOT NULL;

    INSERT INTO ProductVariants (ProductId, Sku, Name, PriceOverride, StockQuantity, IsActive, CreatedAt, UpdatedAt)
    SELECT
        Id,
        CASE
            WHEN NULLIF(BaseSku, '') IS NOT NULL THEN CONCAT(BaseSku, '-', Id)
            ELSE CONCAT('BASE-', Id)
        END,
        'Default',
        NULL,
        Stock,
        IsActive,
        CreatedAt,
        UpdatedAt
    FROM Products;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [Promotions] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(40) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [DiscountType] int NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NULL,
        [MinimumSpend] decimal(18,2) NULL,
        [UsageLimit] int NULL,
        [UsageCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Promotions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [RefreshSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [FamilyId] nvarchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedBySessionId] bigint NULL,
        [UserAgent] nvarchar(300) NULL,
        [IpAddress] nvarchar(64) NULL,
        [LastUsedAt] datetime2 NULL,
        CONSTRAINT [PK_RefreshSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshSessions_RefreshSessions_ReplacedBySessionId] FOREIGN KEY ([ReplacedBySessionId]) REFERENCES [RefreshSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RefreshSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [Shipments] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [Method] nvarchar(120) NULL,
        [Status] int NOT NULL,
        [Courier] nvarchar(120) NULL,
        [TrackingNumber] nvarchar(120) NULL,
        [ShippedAt] datetime2 NULL,
        [DeliveredAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Shipments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [ShippingZones] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_ShippingZones] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [ProductCollections] (
        [ProductId] int NOT NULL,
        [CollectionId] int NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ProductCollections] PRIMARY KEY ([ProductId], [CollectionId]),
        CONSTRAINT [FK_ProductCollections_Collections_CollectionId] FOREIGN KEY ([CollectionId]) REFERENCES [Collections] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductCollections_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Slug] nvarchar(140) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ParentCategoryId] int NULL,
        [ImageMediaId] int NULL,
        [SortOrder] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Categories_ProductMedia_ImageMediaId] FOREIGN KEY ([ImageMediaId]) REFERENCES [ProductMedia] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    WITH LegacyCategories AS
    (
        SELECT
            Category AS Name,
            ROW_NUMBER() OVER (ORDER BY Category) AS SortOrder
        FROM Products
        WHERE NULLIF(Category, '') IS NOT NULL
        GROUP BY Category
    )
    INSERT INTO Categories (Name, Slug, Description, ParentCategoryId, ImageMediaId, SortOrder, IsActive, CreatedAt, UpdatedAt)
    SELECT
        Name,
        CONCAT(
            LOWER(REPLACE(REPLACE(REPLACE(Name, ' ', '-'), '/', '-'), '&', 'and')),
            '-',
            SortOrder
        ),
        'Migrated from legacy Product.Category',
        NULL,
        NULL,
        SortOrder,
        CAST(1 AS bit),
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM LegacyCategories;

    UPDATE p
    SET CategoryId = c.Id
    FROM Products p
    INNER JOIN Categories c ON c.Name = p.Category
    WHERE NULLIF(p.Category, '') IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [ShippingMethods] (
        [Id] int NOT NULL IDENTITY,
        [ShippingZoneId] int NULL,
        [Name] nvarchar(120) NOT NULL,
        [BasePrice] decimal(18,2) NOT NULL,
        [FreeShippingThreshold] decimal(18,2) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_ShippingMethods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShippingMethods_ShippingZones_ShippingZoneId] FOREIGN KEY ([ShippingZoneId]) REFERENCES [ShippingZones] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [CustomDesignAssets] (
        [Id] int NOT NULL IDENTITY,
        [CustomDesignId] int NOT NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [AssetType] nvarchar(80) NOT NULL,
        [OriginalFileName] nvarchar(255) NULL,
        [ContentType] nvarchar(120) NULL,
        [SizeBytes] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomDesignAssets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE TABLE [CustomDesigns] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [Status] int NOT NULL,
        [DesignJson] nvarchar(max) NOT NULL,
        [Width] decimal(18,2) NOT NULL,
        [Height] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [MaterialCode] nvarchar(120) NULL,
        [FinishCode] nvarchar(120) NULL,
        [PreviewMediaId] int NULL,
        [CalculatedPrice] decimal(18,2) NULL,
        [PricingRuleVersion] nvarchar(80) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomDesigns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomDesigns_CustomDesignAssets_PreviewMediaId] FOREIGN KEY ([PreviewMediaId]) REFERENCES [CustomDesignAssets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomDesigns_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Products_IsActive] ON [Products] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_Slug] ON [Products] ([Slug]) WHERE [Slug] <> ''''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_OrderItems_CustomDesignId] ON [OrderItems] ([CustomDesignId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductVariantId] ON [OrderItems] ([ProductVariantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Addresses_UserId] ON [Addresses] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Addresses_UserId_IsDefault] ON [Addresses] ([UserId], [IsDefault]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId] ON [AuditLogs] ([ActorUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [AuditLogs] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Categories_ImageMediaId] ON [Categories] ([ImageMediaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Categories_IsActive_SortOrder] ON [Categories] ([IsActive], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Collections_IsActive_SortOrder] ON [Collections] ([IsActive], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Collections_Slug] ON [Collections] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_CustomDesignAssets_CustomDesignId] ON [CustomDesignAssets] ([CustomDesignId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_CustomDesigns_PreviewMediaId] ON [CustomDesigns] ([PreviewMediaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_CustomDesigns_Status] ON [CustomDesigns] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_CustomDesigns_UserId] ON [CustomDesigns] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_ChangedByUserId] ON [OrderStatusHistories] ([ChangedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_OrderId_CreatedAt] ON [OrderStatusHistories] ([OrderId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_Provider_ProviderTransactionId] ON [Payments] ([Provider], [ProviderTransactionId]) WHERE [ProviderTransactionId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Payments_Status] ON [Payments] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_ProductCollections_CollectionId_SortOrder] ON [ProductCollections] ([CollectionId], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_ProductMedia_ProductId_IsPrimary] ON [ProductMedia] ([ProductId], [IsPrimary]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_ProductMedia_ProductId_SortOrder] ON [ProductMedia] ([ProductId], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_ProductVariants_ProductId_IsActive] ON [ProductVariants] ([ProductId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductVariants_Sku] ON [ProductVariants] ([Sku]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Promotions_Code] ON [Promotions] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Promotions_IsActive_StartsAt_EndsAt] ON [Promotions] ([IsActive], [StartsAt], [EndsAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_RefreshSessions_ExpiresAt] ON [RefreshSessions] ([ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_RefreshSessions_ReplacedBySessionId] ON [RefreshSessions] ([ReplacedBySessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshSessions_TokenHash] ON [RefreshSessions] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_RefreshSessions_UserId_FamilyId] ON [RefreshSessions] ([UserId], [FamilyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Shipments_OrderId] ON [Shipments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_Shipments_TrackingNumber] ON [Shipments] ([TrackingNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE INDEX [IX_ShippingMethods_ShippingZoneId_IsActive] ON [ShippingMethods] ([ShippingZoneId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingZones_Name] ON [ShippingZones] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_CustomDesigns_CustomDesignId] FOREIGN KEY ([CustomDesignId]) REFERENCES [CustomDesigns] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    ALTER TABLE [CustomDesignAssets] ADD CONSTRAINT [FK_CustomDesignAssets_CustomDesigns_CustomDesignId] FOREIGN KEY ([CustomDesignId]) REFERENCES [CustomDesigns] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003072610_Phase1FoundationDataModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003072610_Phase1FoundationDataModel', N'8.0.10');
END;
GO

COMMIT;
GO

