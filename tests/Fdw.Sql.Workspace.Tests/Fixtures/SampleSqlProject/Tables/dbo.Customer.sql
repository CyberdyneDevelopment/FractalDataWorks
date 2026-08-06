CREATE TABLE [dbo].[Customer]
(
    [CustomerId]  INT IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(200)     NOT NULL,
    [Email]       NVARCHAR(320)     NULL,
    [CreatedAt]   DATETIME2         NOT NULL CONSTRAINT [DF_Customer_CreatedAt] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED ([CustomerId])
);
