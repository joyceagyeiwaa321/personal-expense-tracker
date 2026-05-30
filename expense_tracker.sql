-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: May 30, 2026 at 07:45 PM
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

--
-- Dumping data for table `account`
--

INSERT INTO `account` (`AccountID`, `UserID`, `Name`, `AccountType`, `Balance`, `Currency`, `CreatedAt`) VALUES
(1, 1, 'Main Wallet', 'Personal', 650.00, 'EUR', '2026-04-29 23:27:42'),
(10, 14, 'jgjhb', 'Checking', 466.00, 'EUR', '2026-05-30 00:22:02'),
(11, 14, 'Savings mine', 'Savings', 29.00, 'EUR', '2026-05-30 13:33:24');

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

--
-- Dumping data for table `budget`
--

INSERT INTO `budget` (`BudgetID`, `UserID`, `CategoryID`, `LimitAmount`, `Month`) VALUES
(5, 14, 5, 50.00, '2026-05'),
(6, 14, 1, 150.00, '2026-05');

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

--
-- Dumping data for table `category`
--

INSERT INTO `category` (`CategoryID`, `UserID`, `Name`, `Type`, `IsDefault`) VALUES
(1, 1, 'Food & Dining', 'expense', 1),
(3, 1, 'Housing', 'expense', 1),
(4, 1, 'Health', 'expense', 1),
(5, 1, 'Entertainment', 'Expense', 1),
(6, 1, 'Salary', 'income', 1),
(7, 1, 'Groceries', 'expense', 0),
(35, 14, 'Bus', 'Expense', 0);

-- --------------------------------------------------------

--
-- Table structure for table `expense_split`
--

CREATE TABLE `expense_split` (
  `ExpenseSplitID` int(11) NOT NULL,
  `TransactionID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Amount` decimal(10,2) DEFAULT NULL,
  `IsPaid` tinyint(1) DEFAULT 0,
  `PaidAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `expense_split`
--

INSERT INTO `expense_split` (`ExpenseSplitID`, `TransactionID`, `UserID`, `Amount`, `IsPaid`, `PaidAt`) VALUES
(1, 355, 14, 50.00, 1, '2026-05-30 14:00:15'),
(2, 356, 14, 89789.00, 1, '2026-05-30 14:01:48'),
(3, 428, 14, 60.00, 1, '2026-05-30 16:59:45');

-- --------------------------------------------------------

--
-- Table structure for table `goal`
--

CREATE TABLE `goal` (
  `GoalID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `TargetAmount` decimal(10,2) DEFAULT NULL,
  `SavedAmount` decimal(10,2) DEFAULT 0.00,
  `Deadline` datetime DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `goal`
--

INSERT INTO `goal` (`GoalID`, `UserID`, `Name`, `TargetAmount`, `SavedAmount`, `Deadline`, `CreatedAt`) VALUES
(1, 14, 'Slay', 200.00, 29.00, '2026-05-31 00:00:00', '2026-05-30 13:35:36'),
(2, 14, 'yes', 250.00, 5.00, '2026-05-30 00:00:00', '2026-05-30 13:44:55'),
(3, 14, 'no', 100.00, 25.00, '2026-05-30 00:00:00', '2026-05-30 13:54:29');

-- --------------------------------------------------------

--
-- Table structure for table `group`
--

CREATE TABLE `group` (
  `GroupID` int(11) NOT NULL,
  `CreatedByUserID` int(11) NOT NULL,
  `Name` varchar(45) NOT NULL,
  `InviteCode` varchar(6) NOT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Description` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `group`
--

INSERT INTO `group` (`GroupID`, `CreatedByUserID`, `Name`, `InviteCode`, `CreatedAt`, `Description`) VALUES
(3, 5, 'Family Budget', '9X3JP3', '2026-04-30 11:37:41', 'Shared family expenses'),
(4, 14, 'Going out with Joyce;)', '0GEYSY', '2026-05-30 13:39:54', ''),
(5, 14, 'jhsdg', '6LK72I', '2026-05-30 16:56:19', ''),
(6, 14, 'sjhdg', 'BWHHET', '2026-05-30 16:59:35', ''),
(7, 14, 'Going Out', 'Y1ZHRM', '2026-05-30 18:17:27', '');

-- --------------------------------------------------------

--
-- Table structure for table `groupinvites`
--

CREATE TABLE `groupinvites` (
  `InviteID` int(11) NOT NULL,
  `GroupID` int(11) NOT NULL,
  `FromUserID` int(11) NOT NULL,
  `ToUserID` int(11) NOT NULL,
  `Status` varchar(20) DEFAULT 'Pending',
  `SentAt` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `group_member`
--

CREATE TABLE `group_member` (
  `GroupID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `JoinedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `group_member`
--

INSERT INTO `group_member` (`GroupID`, `UserID`, `JoinedAt`) VALUES
(3, 5, '2026-04-30 11:37:41'),
(7, 14, '2026-05-30 18:17:27');

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

--
-- Dumping data for table `recurring_transaction`
--

INSERT INTO `recurring_transaction` (`RecurringID`, `AccountID`, `CategoryID`, `Type`, `Amount`, `Frequency`, `StartDate`, `NextRunDate`, `IsActive`) VALUES
(6, 10, 5, 'Expense', 558.00, 'Weekly', '2026-05-28 00:00:00', '2026-06-04 00:00:00', 1),
(7, 11, 35, 'Expense', 20.00, 'Monthly', '2026-05-30 00:00:00', '2026-06-30 00:00:00', 1),
(8, 10, 35, 'Expense', 60.00, 'Monthly', '2026-05-30 00:00:00', '2026-06-30 00:00:00', 1),
(9, 11, 35, 'Expense', 2.50, 'Weekly', '2026-05-30 00:00:00', '2026-06-06 00:00:00', 1),
(10, 11, 35, 'Expense', 67.00, 'Monthly', '2026-05-29 00:00:00', '2026-06-29 00:00:00', 1);

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
  `Date` datetime DEFAULT NULL,
  `GroupID` int(11) DEFAULT NULL,
  `ReceiptID` int(11) DEFAULT NULL,
  `Status` varchar(20) DEFAULT 'Pending'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `transaction`
--

INSERT INTO `transaction` (`TransactionID`, `UserID`, `CategoryID`, `AccountID`, `Type`, `Amount`, `Description`, `Date`, `GroupID`, `ReceiptID`, `Status`) VALUES
(434, 14, 35, 10, 'Expense', 67.00, 'shdgfvs', '2026-05-30 00:00:00', NULL, NULL, 'Pending'),
(435, 14, 6, 10, 'Income', 50.00, 'Uni', '2026-05-30 00:00:00', NULL, NULL, 'Pending');

-- --------------------------------------------------------

--
-- Table structure for table `user`
--

CREATE TABLE `user` (
  `UserID` int(11) NOT NULL,
  `Username` varchar(45) DEFAULT NULL,
  `Email` varchar(255) NOT NULL,
  `Password` varchar(255) NOT NULL,
  `Role` varchar(45) DEFAULT NULL,
  `IsActive` tinyint(4) DEFAULT NULL,
  `ResetToken` varchar(255) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `IsVerified` tinyint(1) DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `user`
--

INSERT INTO `user` (`UserID`, `Username`, `Email`, `Password`, `Role`, `IsActive`, `ResetToken`, `CreatedAt`, `IsVerified`) VALUES
(1, 'TestUser', 'namoqa0@gmail.com', '$2a$11$dHaVGRmHcYPAPVIGheF0CehAHe/Uhk3tgfe8lBKTQtoTra8zwGne2', 'Admin', 1, 'BF7A0D', '2026-04-29 23:26:09', 1),
(5, 'PersistentUser', 'hellokittyy2016@yahoo.com', '$2a$11$4hW6OahF.zrFmW1b23K./.mRKY3DMj9QPTY.bD5SQM6UhuZbc.M.m', 'User', 1, 'BE490A', '2026-04-30 11:36:52', 0),
(14, 'nemo', 'chimc8699@gmail.com', '$2a$11$7CWsjS9sbJ4KKFLwHBXaGeb/kFaJVh7RolbSxQeKqN9/KvrK2T0iu', 'User', 1, NULL, '2026-05-29 15:23:33', 0),
(15, 'Student', 'r1074198@student.thomasmore.be', '$2a$11$8JsAP.7Mp2jzytEKyrxo/.1L6fk.A2urKveDfcvy91ENtEq9E66pC', 'User', 1, NULL, '2026-05-30 18:26:54', 0);

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
  `PreferedCurrency` varchar(10) DEFAULT NULL,
  `NotifGoalReminders` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `user_profile`
--

INSERT INTO `user_profile` (`ProfileID`, `UserID`, `FirstName`, `LastName`, `PhoneNumber`, `AvatarURL`, `PreferedCurrency`, `NotifGoalReminders`) VALUES
(1, 1, 'Updated', 'User', '+61 0123456789', 'C:/Users/namoq/AppData/Roaming/FinancyApplication/Avatars/avatar_admin_1.jpg', 'USD', 1),
(11, 14, 'Namais', 'NY', '+247 8767', 'C:/Users/namoq/AppData/Roaming/FinancyApplication/Avatars/avatar_14.jpg', 'EUR', 1);

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
-- Indexes for table `expense_split`
--
ALTER TABLE `expense_split`
  ADD PRIMARY KEY (`ExpenseSplitID`);

--
-- Indexes for table `goal`
--
ALTER TABLE `goal`
  ADD PRIMARY KEY (`GoalID`);

--
-- Indexes for table `group`
--
ALTER TABLE `group`
  ADD PRIMARY KEY (`GroupID`),
  ADD KEY `fk_Group_User1_idx` (`CreatedByUserID`);

--
-- Indexes for table `groupinvites`
--
ALTER TABLE `groupinvites`
  ADD PRIMARY KEY (`InviteID`);

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
  ADD KEY `fk_Transaction_User1_idx` (`UserID`),
  ADD KEY `fk_Transaction_Group` (`GroupID`);

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
  MODIFY `AccountID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=12;

--
-- AUTO_INCREMENT for table `budget`
--
ALTER TABLE `budget`
  MODIFY `BudgetID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT for table `category`
--
ALTER TABLE `category`
  MODIFY `CategoryID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=38;

--
-- AUTO_INCREMENT for table `expense_split`
--
ALTER TABLE `expense_split`
  MODIFY `ExpenseSplitID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `goal`
--
ALTER TABLE `goal`
  MODIFY `GoalID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `group`
--
ALTER TABLE `group`
  MODIFY `GroupID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT for table `groupinvites`
--
ALTER TABLE `groupinvites`
  MODIFY `InviteID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `receipt`
--
ALTER TABLE `receipt`
  MODIFY `ReceiptID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=9;

--
-- AUTO_INCREMENT for table `recurring_transaction`
--
ALTER TABLE `recurring_transaction`
  MODIFY `RecurringID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT for table `transaction`
--
ALTER TABLE `transaction`
  MODIFY `TransactionID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=436;

--
-- AUTO_INCREMENT for table `user`
--
ALTER TABLE `user`
  MODIFY `UserID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=16;

--
-- AUTO_INCREMENT for table `user_profile`
--
ALTER TABLE `user_profile`
  MODIFY `ProfileID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=12;

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
  ADD CONSTRAINT `fk_Transaction_Group` FOREIGN KEY (`GroupID`) REFERENCES `group` (`GroupID`) ON DELETE SET NULL ON UPDATE NO ACTION,
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
