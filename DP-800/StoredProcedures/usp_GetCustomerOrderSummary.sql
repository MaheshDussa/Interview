/*===============================================================================
 Procedure : usp_GetCustomerOrderSummary
 Purpose   : Retrieves a summary of customer order activity from the
             AdventureWorksLT database, including total order count, total
             order amount, and the date of the most recent order. When a
             @CustomerID is supplied, the result is filtered to that customer;
             otherwise a summary is returned for all customers that have orders.
 Author    : GitHub Copilot
 Database  : AdventureWorksLT
===============================================================================*/
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerOrderSummary
    @CustomerID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Aggregate order detail amounts up to the order header, then to the customer.
        -- Using SalesOrderDetail ensures the total reflects line-level amounts
        -- (UnitPrice * OrderQty net of discounts via LineTotal).
        SELECT
            c.CustomerID,
            c.FirstName + ISNULL(' ' + c.MiddleName, '') + ' ' + c.LastName AS CustomerName,
            COUNT(DISTINCT soh.SalesOrderID)                                AS TotalOrders,
            ISNULL(SUM(sod.LineTotal), 0)                                   AS TotalOrderAmount,
            MAX(soh.OrderDate)                                              AS LastOrderDate
        FROM SalesLT.Customer AS c
        INNER JOIN SalesLT.SalesOrderHeader AS soh
            ON soh.CustomerID = c.CustomerID
        INNER JOIN SalesLT.SalesOrderDetail AS sod
            ON sod.SalesOrderID = soh.SalesOrderID
        WHERE (@CustomerID IS NULL OR c.CustomerID = @CustomerID)
        GROUP BY
            c.CustomerID,
            c.FirstName,
            c.MiddleName,
            c.LastName
        ORDER BY CustomerName;
    END TRY
    BEGIN CATCH
        -- Surface error details to the caller while preserving the original severity.
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO
