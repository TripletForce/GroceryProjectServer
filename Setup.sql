/******************
 * Set up Database
 ******************/
CREATE DATABASE IF NOT EXISTS Tracker;
USE Tracker;

/*********************
 * Drop Tables
 *********************/

DROP TABLE IF EXISTS Tracker.User;
DROP TABLE IF EXISTS Tracker.Recipt;
DROP TABLE IF EXISTS Tracker.Item;
DROP TABLE IF EXISTS Tracker.Store;

/******************
 * Create Tables
 ******************/
CREATE TABLE User
(
   UserId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   Email NVARCHAR(128) NOT NULL,
   JoinDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
   Password NVARCHAR(128) NOT NULL
);
CREATE TABLE Store
(
   StoreId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   Name NVARCHAR(128) NOT NULL,
   State NVARCHAR(2) NOT NULL,
   City NVARCHAR(128) NOT NULL,
   PostalCode NVARCHAR(128) NOT NULL,
   Address NVARCHAR(128) UNIQUE NOT NULL
);
CREATE TABLE Item
(
   ItemId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   Name NVARCHAR(128) NOT NULL,
   Price DECIMAL NOT NULL,
   Quantity INT NOT NULL
);
CREATE TABLE Recipt
(
   ReciptId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
   
   Date DATETIME NOT NULL,
   Subtotal DECIMAL NOT NULL,
   Tax DECIMAL NOT NULL,
   Total DECIMAL NOT NULL,
   PhoneNumber NVARCHAR(10) NOT NULL
   /*Change payment type to an enum table*/
);        

INSERT INTO User (Email, Password) VALUES ("admin", "password");
SHOW databases;