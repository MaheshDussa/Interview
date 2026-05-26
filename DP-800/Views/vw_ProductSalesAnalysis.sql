/*===============================================================================
 View      : vw_ProductSalesAnalysis
 Purpose   : Provides a per-product sales analysis from the AdventureWorksLT
             database, including total quantity sold, total revenue, average
             sale price, and the number of distinct orders containing the
             product. Products that have never been sold are excluded.
 Author    : GitHub Copilot
 Database  : AdventureWorksLT
===============================================================================*/
CREATE OR ALTER VIEW SalesLT.vw_ProductSalesAnalysis
AS
SELECT
    p.ProductID,
    p.Name                                  AS ProductName,
    pc.Name                                 AS CategoryName,
    SUM(sod.OrderQty)                       AS TotalQuantitySold,
    SUM(sod.LineTotal)                      AS TotalRevenue,
    -- Weighted average unit sale price across all line items.
    CAST(SUM(sod.LineTotal) / NULLIF(SUM(sod.OrderQty), 0) AS DECIMAL(19, 4))
                                            AS AverageSalePrice,
    COUNT(DISTINCT sod.SalesOrderID)        AS OrderCount
FROM SalesLT.Product AS p
INNER JOIN SalesLT.ProductCategory AS pc
    ON pc.ProductCategoryID = p.ProductCategoryID
INNER JOIN SalesLT.SalesOrderDetail AS sod
    ON sod.ProductID = p.ProductID
GROUP BY
    p.ProductID,
    p.Name,
    pc.Name;
GO
