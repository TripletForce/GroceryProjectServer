/******************
 * Set up Database
 ******************/
CREATE DATABASE IF NOT EXISTS Tracker;
USE Tracker;

/*********************
 * Drop Tables
 *********************/

DROP TABLE IF EXISTS Tracker.Items;
DROP TABLE IF EXISTS Tracker.Receipts;
DROP TABLE IF EXISTS Tracker.Stores;
DROP TABLE IF EXISTS Tracker.Users;

/******************
 * Create Tables
 ******************/
CREATE TABLE Users
(
   UserId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   Email NVARCHAR(128) NOT NULL UNIQUE,
   JoinDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
   Password NVARCHAR(128) NOT NULL
);
CREATE TABLE Stores
(
   StoreId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   Name NVARCHAR(128) NOT NULL,
   State NVARCHAR(2) NOT NULL,
   City NVARCHAR(128) NOT NULL,
   PostalCode NVARCHAR(128) NOT NULL,
   Address NVARCHAR(128) UNIQUE NOT NULL UNIQUE
);
CREATE TABLE Receipts
(
   ReceiptId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   StoreId INT NOT NULL,
   UserId INT NOT NULL,
   ReceiptDate DATETIME NOT NULL,
   Subtotal DECIMAL NOT NULL,
   Tax DECIMAL NOT NULL,
   Total DECIMAL NOT NULL,
   PhoneNumber NVARCHAR(10) NOT NULL,
   PaymentType ENUM('Debit', 'Credit', 'Cash'),
   
	FOREIGN KEY(StoreId) REFERENCES Stores(StoreId),
	FOREIGN KEY(UserId) REFERENCES Users(UserId)
);        
CREATE TABLE Items
(
   ItemId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   ReceiptId INT NOT NULL,
   Name NVARCHAR(128) NOT NULL,
   Price DECIMAL NOT NULL,
   Quantity INT NOT NULL,
   FOREIGN KEY(ReceiptId) REFERENCES Receipts(ReceiptId)
);
INSERT INTO Users (Email, Password) VALUES ("admin", "password");
SHOW databases;