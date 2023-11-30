/*********************
 * Insert statements
 *********************/
 
 /* Insert store */
INSERT INTO Stores(Name, State, City, PostalCode, Address) VALUES(?name, ?state, ?city, ?postal, ?address)
ON DUPLICATE KEY UPDATE 
Name = ?name, State = ?state, City = ?city, PostalCode = ?postal, Address = ?address;

/* Gets the latest StoreId*/
SELECT StoreId FROM tracker.stores WHERE Address = ?address;

/* Insert Recipt */
INSERT INTO Receipts(StoreId, UserId, ReceiptDate, Subtotal, Tax, Total, PhoneNumber, PaymentType) 
VALUE (?store_id, ?user, CURRENT_TIMESTAMP, ?sub_total, ?tax, ?total, ?phone, ?payment);

/* Insert Item */
INSERT INTO Items(ReceiptId, Name, Price, Quantity) 
VALUE (?receipt_id, ?name, ?price, ?quantity);

