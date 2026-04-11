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