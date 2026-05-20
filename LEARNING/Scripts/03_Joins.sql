/* =============================================================================
   Script3.sql  -  JOINS in T-SQL
   -----------------------------------------------------------------------------
   The five JOIN flavors at a glance (Venn-diagram mental model):

      INNER JOIN       -> rows that have a match on BOTH sides
      LEFT  [OUTER]    -> ALL rows from LEFT  + matches from right (unmatched right cols = NULL)
      RIGHT [OUTER]    -> ALL rows from RIGHT + matches from left  (unmatched left  cols = NULL)
      FULL  [OUTER]    -> everything from both sides; unmatched cols on either side are NULL
      CROSS JOIN       -> Cartesian product (every left row paired with every right row;
                          NO  ON clause)

   Interview talking points:
     * "OUTER" keyword is optional - LEFT JOIN == LEFT OUTER JOIN.
     * Putting a filter on the OUTER table in the WHERE clause silently turns
       a LEFT/RIGHT JOIN back into an INNER JOIN. Push such filters into the
       ON clause to preserve the outer rows.
     * The DATASET in this DB was seeded with deliberate gaps:
         - Lisa, Mark, Rachel have NO roles            -> visible via LEFT  JOIN
         - Guest and Auditor roles have NO users       -> visible via RIGHT JOIN
       This makes the JOIN behavior easy to observe.
   ============================================================================= */


-- -----------------------------------------------------------------------------
-- 1) INNER JOIN  -  only users that ACTUALLY have a role assigned.
--    Users without any USER_ROLES row are silently dropped.
-- -----------------------------------------------------------------------------
SELECT u.FirstName, u.LastName, r.RoleName
FROM [USERS] u
INNER JOIN [USER_ROLES] ur ON u.UserId = ur.UserId    -- bridge table (M:N resolver)
INNER JOIN [ROLES]      r  ON ur.RoleId = r.RoleId;


-- -----------------------------------------------------------------------------
-- 2) LEFT JOIN  -  every user, even those without roles.
--    For Lisa / Mark / Rachel, RoleName will be NULL.
--    Use LEFT JOIN whenever the LEFT table is the "primary" data and the
--    right table is just optional enrichment.
-- -----------------------------------------------------------------------------
SELECT u.FirstName, u.LastName, r.RoleName
FROM [USERS] u
LEFT JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
LEFT JOIN [ROLES]      r  ON ur.RoleId = r.RoleId;


-- -----------------------------------------------------------------------------
-- 3) RIGHT JOIN  -  driven by the RIGHT-most table (ROLES).
--    Because the second RIGHT JOIN keeps ALL roles, any role with no users
--    (Guest, Auditor) will still appear, with FirstName / LastName as NULL.
--    Note: RIGHT JOIN is functionally equivalent to swapping the table order
--    and using LEFT JOIN - most teams prefer LEFT JOIN for readability.
-- -----------------------------------------------------------------------------
SELECT u.FirstName, u.LastName, r.RoleName
FROM [USERS] u
RIGHT JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
RIGHT JOIN [ROLES]      r  ON ur.RoleId = r.RoleId;


-- -----------------------------------------------------------------------------
-- 4) FULL OUTER JOIN  -  union of LEFT and RIGHT: keeps unmatched rows
--    from BOTH sides. Useful for reconciliation / "what's missing where?".
--    Users with no roles AND roles with no users will both appear.
-- -----------------------------------------------------------------------------
SELECT u.FirstName, u.LastName, r.RoleName
FROM [USERS] u
FULL OUTER JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
FULL OUTER JOIN [ROLES]      r  ON ur.RoleId = r.RoleId;


-- -----------------------------------------------------------------------------
-- 5) CROSS JOIN  -  Cartesian product: every USER paired with every ROLE.
--    Row count = COUNT(USERS) * COUNT(ROLES). No ON clause.
--    Real-world uses: generating calendars, test data, or building a full
--    matrix to LEFT JOIN against (e.g. find missing combinations).
--    Warning: easy to blow up result size if either table is large.
-- -----------------------------------------------------------------------------
SELECT u.FirstName, r.RoleName
FROM [USERS] u
CROSS JOIN [ROLES] r;


-- -----------------------------------------------------------------------------
-- BONUS - common interview follow-ups
-- -----------------------------------------------------------------------------

-- a) Find users who have NO role (anti-join pattern using LEFT JOIN + IS NULL).
SELECT u.FirstName, u.LastName
FROM [USERS] u
LEFT JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
WHERE ur.UserId IS NULL;

-- b) Find roles that are NOT assigned to any user (anti-join the other way).
SELECT r.RoleName
FROM [ROLES] r
LEFT JOIN [USER_ROLES] ur ON r.RoleId = ur.RoleId
WHERE ur.RoleId IS NULL;

-- c) SELF JOIN  - joining a table to itself. Classic example: pair up users
--    to compare them. Use DIFFERENT aliases for each instance, and add a
--    predicate (u1.UserId < u2.UserId) to avoid duplicate pairs / self-pairs.
SELECT u1.FirstName AS User1, u2.FirstName AS User2
FROM [USERS] u1
INNER JOIN [USERS] u2 ON u1.UserId < u2.UserId;

