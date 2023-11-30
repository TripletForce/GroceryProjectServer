/*********************
 * Aggregating Queries
 * Note that any ?property is substitued by the server MySql library in C#
 * This matches the doc order
 *********************/

/* Gets the user information from the database */
SELECT U.UserId,
	U.Email,
	DATE(U.JoinDate) AS JoinDate,
	COALESCE(SUM(I.Price*I.Quantity), 0) AS Spent,
	COUNT(DISTINCT R.ReceiptId) AS UploadedRecipts
FROM tracker.users U
	LEFT JOIN tracker.receipts R ON R.UserId = U.UserId
	LEFT JOIN tracker.items I ON I.ReceiptId = R.ReceiptId
WHERE U.UserId = ?user_id
GROUP BY U.Email, U.JoinDate;

/* Gets stores ranked by sales */
SELECT S.Name,
	S.Address,
	MIN(R.PhoneNumber) AS Phone,
	COALESCE(SUM(I.Price*I.Quantity), 0) AS Spent
FROM tracker.stores S
	LEFT JOIN tracker.receipts R ON R.StoreId = S.StoreId
	LEFT JOIN tracker.items I ON I.ReceiptId = R.ReceiptId
GROUP BY S.Name, S.Address;

/* Ranks all of the users based by amount spent */
SELECT U.Email,
	COALESCE(SUM(I.Price*I.Quantity), 0) AS Spent
FROM tracker.users U
	LEFT JOIN tracker.receipts R ON R.UserId = U.UserId
	LEFT JOIN tracker.items I ON I.ReceiptId = R.ReceiptId
GROUP BY U.Email;

/* Gets bought items from user ranked by cost over all recipts */
SELECT Name,
	SUM(Price*Quantity) AS Spent
FROM tracker.items I
	INNER JOIN tracker.receipts R ON I.ReceiptId = R.ReceiptId
WHERE R.UserId = ?user_id
GROUP BY Name
ORDER BY Spent DESC;