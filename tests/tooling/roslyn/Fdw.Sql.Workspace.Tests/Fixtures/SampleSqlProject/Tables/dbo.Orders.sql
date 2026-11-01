CREATE TABLE [dbo].[Orders]
(
    [OrderId]    INT IDENTITY(1,1) NOT NULL,
    [CustomerId] INT               NOT NULL,
    [Total]      DECIMAL(10,2)     NOT NULL,
    [PlacedAt]   DATETIME2         NOT NULL CONSTRAINT [DF_Orders_PlacedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([OrderId]),
    CONSTRAINT [FK_Orders_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([CustomerId])
);
