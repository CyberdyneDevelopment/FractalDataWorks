CREATE PROCEDURE [dbo].[GetCustomerById]
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [CustomerId], [Name], [Email], [CreatedAt]
    FROM [dbo].[Customer]
    WHERE [CustomerId] = @CustomerId;
END;
