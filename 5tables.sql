-- Create the database first
CREATE SCHEMA IF NOT EXISTS `mydb`;
USE `mydb`;

-- Now run the rest of the script
SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Table `User`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `User` ;

CREATE TABLE IF NOT EXISTS `User` (
  `UserID` INT NOT NULL AUTO_INCREMENT,
  `Username` VARCHAR(45) NULL,
  `Email` VARCHAR(255) NOT NULL,
  `PasswordHash` VARCHAR(255) NOT NULL,
  `Role` VARCHAR(45) NULL,
  `IsActive` TINYINT NULL,
  `ResetToken` VARCHAR(255) NULL,
  `CreatedAt` DATETIME NULL,
  PRIMARY KEY (`UserID`))
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `User_Profile`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `User_Profile` ;

CREATE TABLE IF NOT EXISTS `User_Profile` (
  `ProfileID` INT NOT NULL AUTO_INCREMENT,
  `UserID` INT NOT NULL,
  `FirstName` VARCHAR(45) NOT NULL,
  `LastName` VARCHAR(45) NOT NULL,
  `PhoneNumber` VARCHAR(20) NULL,
  `AvatarURL` VARCHAR(255) NULL,
  `PreferedCurrency` VARCHAR(10) NULL,
  PRIMARY KEY (`ProfileID`),
  INDEX `fk_User_Profile_User_idx` (`UserID` ASC),
  CONSTRAINT `fk_User_Profile_User`
    FOREIGN KEY (`UserID`)
    REFERENCES `User` (`UserID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `Account`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Account` ;

CREATE TABLE IF NOT EXISTS `Account` (
  `AccountID` INT NOT NULL AUTO_INCREMENT,
  `UserID` INT NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `AccountType` VARCHAR(45) NULL,
  `Balance` DECIMAL(10,2) NULL,
  `Currency` VARCHAR(10) NULL,
  `CreatedAt` DATETIME NULL,
  PRIMARY KEY (`AccountID`),
  INDEX `fk_Account_User1_idx` (`UserID` ASC),
  CONSTRAINT `fk_Account_User1`
    FOREIGN KEY (`UserID`)
    REFERENCES `User` (`UserID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `Category`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Category` ;

CREATE TABLE IF NOT EXISTS `Category` (
  `CategoryID` INT NOT NULL AUTO_INCREMENT,
  `UserID` INT NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Type` VARCHAR(45) NULL,
  `IsDefault` TINYINT NULL,
  PRIMARY KEY (`CategoryID`),
  INDEX `fk_Category_User1_idx` (`UserID` ASC),
  CONSTRAINT `fk_Category_User1`
    FOREIGN KEY (`UserID`)
    REFERENCES `User` (`UserID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `Transaction`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Transaction` ;

CREATE TABLE IF NOT EXISTS `Transaction` (
  `TransactionID` INT NOT NULL AUTO_INCREMENT,
  `UserID` INT NOT NULL,
  `CategoryID` INT NOT NULL,
  `AccountID` INT NOT NULL,
  `Type` VARCHAR(45) NULL,
  `Amount` DECIMAL(10,2) NOT NULL,
  `Description` VARCHAR(255) NULL,
  `Date` DATETIME NULL,
  PRIMARY KEY (`TransactionID`),
  INDEX `fk_Transaction_Account1_idx` (`AccountID` ASC),
  INDEX `fk_Transaction_Category1_idx` (`CategoryID` ASC),
  INDEX `fk_Transaction_User1_idx` (`UserID` ASC),
  CONSTRAINT `fk_Transaction_Account1`
    FOREIGN KEY (`AccountID`)
    REFERENCES `Account` (`AccountID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Transaction_Category1`
    FOREIGN KEY (`CategoryID`)
    REFERENCES `Category` (`CategoryID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Transaction_User1`
    FOREIGN KEY (`UserID`)
    REFERENCES `User` (`UserID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;

-- -----------------------------------------------------
-- Table `Budget`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Budget` ;

CREATE TABLE IF NOT EXISTS `Budget` (
  `BudgetID` INT NOT NULL AUTO_INCREMENT,
  `UserID` INT NOT NULL,
  `CategoryID` INT NOT NULL,
  `LimitAmount` DECIMAL(10,2) NOT NULL,
  `Month` VARCHAR(20) NULL,
  PRIMARY KEY (`BudgetID`),
  INDEX `fk_Budget_User1_idx` (`UserID` ASC),
  INDEX `fk_Budget_Category1_idx` (`CategoryID` ASC),
  CONSTRAINT `fk_Budget_User1`
    FOREIGN KEY (`UserID`)
    REFERENCES `User` (`UserID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Budget_Category1`
    FOREIGN KEY (`CategoryID`)
    REFERENCES `Category` (`CategoryID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `Receipt`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Receipt` ;

CREATE TABLE IF NOT EXISTS `Receipt` (
  `ReceiptID` INT NOT NULL AUTO_INCREMENT,
  `TransactionID` INT NOT NULL,
  `FilePath` VARCHAR(255) NOT NULL,
  `FileType` VARCHAR(45) NULL,
  `UploadedAt` DATETIME NULL,
  PRIMARY KEY (`ReceiptID`),
  INDEX `fk_Receipt_Transaction1_idx` (`TransactionID` ASC),
  CONSTRAINT `fk_Receipt_Transaction1`
    FOREIGN KEY (`TransactionID`)
    REFERENCES `Transaction` (`TransactionID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `Recurring_Transaction`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Recurring_Transaction` ;

CREATE TABLE IF NOT EXISTS `Recurring_Transaction` (
  `RecurringID` INT NOT NULL AUTO_INCREMENT,
  `AccountID` INT NOT NULL,
  `CategoryID` INT NOT NULL,
  `Type` VARCHAR(45) NULL,
  `Amount` DECIMAL(10,2) NOT NULL,
  `Frequency` VARCHAR(45) NULL,
  `StartDate` DATETIME NULL,
  `NextRunDate` DATETIME NULL,
  `IsActive` TINYINT NULL,
  PRIMARY KEY (`RecurringID`),
  INDEX `fk_Recurring_Transaction_Account1_idx` (`AccountID` ASC),
  INDEX `fk_Recurring_Transaction_Category1_idx` (`CategoryID` ASC),
  CONSTRAINT `fk_Recurring_Transaction_Account1`
    FOREIGN KEY (`AccountID`)
    REFERENCES `Account` (`AccountID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Recurring_Transaction_Category1`
    FOREIGN KEY (`CategoryID`)
    REFERENCES `Category` (`CategoryID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `Group`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Group` ;

CREATE TABLE IF NOT EXISTS `Group` (
  `GroupID` INT NOT NULL AUTO_INCREMENT,
  `CreatedByUserID` INT NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `CreatedAt` DATETIME NULL,
  PRIMARY KEY (`GroupID`),
  INDEX `fk_Group_User1_idx` (`CreatedByUserID` ASC),
  CONSTRAINT `fk_Group_User1`
    FOREIGN KEY (`CreatedByUserID`)
    REFERENCES `User` (`UserID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

-- -----------------------------------------------------
-- Table `Group_Member`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Group_Member` ;

CREATE TABLE IF NOT EXISTS `Group_Member` (
  `GroupID` INT NOT NULL,
  `UserID` INT NOT NULL,
  `Role` VARCHAR(45) NULL,
  `JoinedAt` DATETIME NULL,
  INDEX `fk_Group_Member_Group1_idx` (`GroupID` ASC),
  INDEX `fk_Group_Member_User1_idx` (`UserID` ASC),
  CONSTRAINT `fk_Group_Member_Group1`
    FOREIGN KEY (`GroupID`)
    REFERENCES `Group` (`GroupID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Group_Member_User1`
    FOREIGN KEY (`UserID`)
    REFERENCES `User` (`UserID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;
