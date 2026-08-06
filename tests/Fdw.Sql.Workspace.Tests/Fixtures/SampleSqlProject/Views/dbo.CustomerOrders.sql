CREATE VIEW [dbo].[CustomerOrders]
AS
    SELECT
        c.[CustomerId],
        c.[Name],
        c.[Email],
        o.[OrderId],
        o.[Total],
        o.[PlacedAt]
    FROM [dbo].[Customer] c
        INNER JOIN [dbo].[Orders] o ON o.[CustomerId] = c.[CustomerId];
