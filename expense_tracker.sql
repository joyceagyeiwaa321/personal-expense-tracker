-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Apr 14, 2026 at 02:50 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `expense_tracker`
--

-- --------------------------------------------------------

--
-- Table structure for table `account`
--

CREATE TABLE `account` (
  `AccountID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Name` varchar(45) NOT NULL,
  `AccountType` varchar(45) DEFAULT NULL,
  `Balance` decimal(10,2) DEFAULT NULL,
  `Currency` varchar(10) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `budget`
--

CREATE TABLE `budget` (
  `BudgetID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `CategoryID` int(11) NOT NULL,
  `LimitAmount` decimal(10,2) NOT NULL,
  `Month` varchar(20) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `category`
--

CREATE TABLE `category` (
  `CategoryID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Name` varchar(45) NOT NULL,
  `Type` varchar(45) DEFAULT NULL,
  `IsDefault` tinyint(4) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `group`
--

CREATE TABLE `group` (
  `GroupID` int(11) NOT NULL,
  `CreatedByUserID` int(11) NOT NULL,
  `Name` varchar(45) NOT NULL,
  `CreatedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `group_member`
--

CREATE TABLE `group_member` (
  `GroupID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Role` varchar(45) DEFAULT NULL,
  `JoinedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `receipt`
--

CREATE TABLE `receipt` (
  `ReceiptID` int(11) NOT NULL,
  `TransactionID` int(11) NOT NULL,
  `FilePath` varchar(255) NOT NULL,
  `FileType` varchar(45) DEFAULT NULL,
  `UploadedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `recurring_transaction`
--

CREATE TABLE `recurring_transaction` (
  `RecurringID` int(11) NOT NULL,
  `AccountID` int(11) NOT NULL,
  `CategoryID` int(11) NOT NULL,
  `Type` varchar(45) DEFAULT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `Frequency` varchar(45) DEFAULT NULL,
  `StartDate` datetime DEFAULT NULL,
  `NextRunDate` datetime DEFAULT NULL,
  `IsActive` tinyint(4) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `transaction`
--

CREATE TABLE `transaction` (
  `TransactionID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `CategoryID` int(11) NOT NULL,
  `AccountID` int(11) NOT NULL,
  `Type` varchar(45) DEFAULT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `Date` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `user`
--

CREATE TABLE `user` (
  `UserID` int(11) NOT NULL,
  `Username` varchar(45) DEFAULT NULL,
  `Email` varchar(255) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `Role` varchar(45) DEFAULT NULL,
  `IsActive` tinyint(4) DEFAULT NULL,
  `ResetToken` varchar(255) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `user_profile`
--

CREATE TABLE `user_profile` (
  `ProfileID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `FirstName` varchar(45) NOT NULL,
  `LastName` varchar(45) NOT NULL,
  `PhoneNumber` varchar(20) DEFAULT NULL,
  `AvatarURL` varchar(255) DEFAULT NULL,
  `PreferedCurrency` varchar(10) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `account`
--
ALTER TABLE `account`
  ADD PRIMARY KEY (`AccountID`),
  ADD KEY `fk_Account_User1_idx` (`UserID`);

--
-- Indexes for table `budget`
--
ALTER TABLE `budget`
  ADD PRIMARY KEY (`BudgetID`),
  ADD KEY `fk_Budget_User1_idx` (`UserID`),
  ADD KEY `fk_Budget_Category1_idx` (`CategoryID`);

--
-- Indexes for table `category`
--
ALTER TABLE `category`
  ADD PRIMARY KEY (`CategoryID`),
  ADD KEY `fk_Category_User1_idx` (`UserID`);

--
-- Indexes for table `group`
--
ALTER TABLE `group`
  ADD PRIMARY KEY (`GroupID`),
  ADD KEY `fk_Group_User1_idx` (`CreatedByUserID`);

--
-- Indexes for table `group_member`
--
ALTER TABLE `group_member`
  ADD KEY `fk_Group_Member_Group1_idx` (`GroupID`),
  ADD KEY `fk_Group_Member_User1_idx` (`UserID`);

--
-- Indexes for table `receipt`
--
ALTER TABLE `receipt`
  ADD PRIMARY KEY (`ReceiptID`),
  ADD KEY `fk_Receipt_Transaction1_idx` (`TransactionID`);

--
-- Indexes for table `recurring_transaction`
--
ALTER TABLE `recurring_transaction`
  ADD PRIMARY KEY (`RecurringID`),
  ADD KEY `fk_Recurring_Transaction_Account1_idx` (`AccountID`),
  ADD KEY `fk_Recurring_Transaction_Category1_idx` (`CategoryID`);

--
-- Indexes for table `transaction`
--
ALTER TABLE `transaction`
  ADD PRIMARY KEY (`TransactionID`),
  ADD KEY `fk_Transaction_Account1_idx` (`AccountID`),
  ADD KEY `fk_Transaction_Category1_idx` (`CategoryID`),
  ADD KEY `fk_Transaction_User1_idx` (`UserID`);

--
-- Indexes for table `user`
--
ALTER TABLE `user`
  ADD PRIMARY KEY (`UserID`);

--
-- Indexes for table `user_profile`
--
ALTER TABLE `user_profile`
  ADD PRIMARY KEY (`ProfileID`),
  ADD KEY `fk_User_Profile_User_idx` (`UserID`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `account`
--
ALTER TABLE `account`
  MODIFY `AccountID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `budget`
--
ALTER TABLE `budget`
  MODIFY `BudgetID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `category`
--
ALTER TABLE `category`
  MODIFY `CategoryID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `group`
--
ALTER TABLE `group`
  MODIFY `GroupID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `receipt`
--
ALTER TABLE `receipt`
  MODIFY `ReceiptID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `recurring_transaction`
--
ALTER TABLE `recurring_transaction`
  MODIFY `RecurringID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `transaction`
--
ALTER TABLE `transaction`
  MODIFY `TransactionID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `user`
--
ALTER TABLE `user`
  MODIFY `UserID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `user_profile`
--
ALTER TABLE `user_profile`
  MODIFY `ProfileID` int(11) NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `account`
--
ALTER TABLE `account`
  ADD CONSTRAINT `fk_Account_User1` FOREIGN KEY (`UserID`) REFERENCES `user` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `budget`
--
ALTER TABLE `budget`
  ADD CONSTRAINT `fk_Budget_Category1` FOREIGN KEY (`CategoryID`) REFERENCES `category` (`CategoryID`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `fk_Budget_User1` FOREIGN KEY (`UserID`) REFERENCES `user` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `category`
--
ALTER TABLE `category`
  ADD CONSTRAINT `fk_Category_User1` FOREIGN KEY (`UserID`) REFERENCES `user` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `group`
--
ALTER TABLE `group`
  ADD CONSTRAINT `fk_Group_User1` FOREIGN KEY (`CreatedByUserID`) REFERENCES `user` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `group_member`
--
ALTER TABLE `group_member`
  ADD CONSTRAINT `fk_Group_Member_Group1` FOREIGN KEY (`GroupID`) REFERENCES `group` (`GroupID`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `fk_Group_Member_User1` FOREIGN KEY (`UserID`) REFERENCES `user` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `receipt`
--
ALTER TABLE `receipt`
  ADD CONSTRAINT `fk_Receipt_Transaction1` FOREIGN KEY (`TransactionID`) REFERENCES `transaction` (`TransactionID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `recurring_transaction`
--
ALTER TABLE `recurring_transaction`
  ADD CONSTRAINT `fk_Recurring_Transaction_Account1` FOREIGN KEY (`AccountID`) REFERENCES `account` (`AccountID`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `fk_Recurring_Transaction_Category1` FOREIGN KEY (`CategoryID`) REFERENCES `category` (`CategoryID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `transaction`
--
ALTER TABLE `transaction`
  ADD CONSTRAINT `fk_Transaction_Account1` FOREIGN KEY (`AccountID`) REFERENCES `account` (`AccountID`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `fk_Transaction_Category1` FOREIGN KEY (`CategoryID`) REFERENCES `category` (`CategoryID`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `fk_Transaction_User1` FOREIGN KEY (`UserID`) REFERENCES `user` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `user_profile`
--
ALTER TABLE `user_profile`
  ADD CONSTRAINT `fk_User_Profile_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
