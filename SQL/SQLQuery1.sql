
CREATE DATABASE VetClinicDB;

USE VetClinicDB;


CREATE TABLE Owners (
    OwnerID INT PRIMARY KEY IDENTITY(1,1),
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100) UNIQUE
);

CREATE TABLE Pets (
    PetID INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(50) NOT NULL,
    Species VARCHAR(30) NOT NULL,
    Breed VARCHAR(50),
    BirthDate DATE,
    OwnerID INT NOT NULL,
    CONSTRAINT FK_Pets_Owners
        FOREIGN KEY (OwnerID)
        REFERENCES Owners(OwnerID)
);


CREATE TABLE Veterinarians (
    VetID INT PRIMARY KEY IDENTITY(1,1),
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Specialty VARCHAR(100)
);


CREATE TABLE Appointments (
    AppointmentID INT PRIMARY KEY IDENTITY(1,1),
    PetID INT NOT NULL,
    VetID INT NOT NULL,
    AppointmentDate DATETIME NOT NULL,
    Diagnosis VARCHAR(255),

    CONSTRAINT FK_Appointments_Pets
        FOREIGN KEY (PetID)
        REFERENCES Pets(PetID),

    CONSTRAINT FK_Appointments_Vets
        FOREIGN KEY (VetID)
        REFERENCES Veterinarians(VetID)
);

CREATE TABLE Treatments (
    TreatmentID INT PRIMARY KEY IDENTITY(1,1),
    AppointmentID INT NOT NULL,
    TreatmentName VARCHAR(100) NOT NULL,
    Price DECIMAL(10,2) CHECK (Price >= 0),

    CONSTRAINT FK_Treatments_Appointments
        FOREIGN KEY (AppointmentID)
        REFERENCES Appointments(AppointmentID)
);



INSERT INTO Owners (FirstName, LastName, Phone, Email)
VALUES
('Иван', 'Петров', '0888123456', 'ivan@gmail.com'),
('Мария', 'Георгиева', '0888234567', 'maria@gmail.com');

INSERT INTO Pets (Name, Species, Breed, BirthDate, OwnerID)
VALUES
('Рекс', 'Куче', 'Немска овчарка', '2021-05-10', 1),
('Мая', 'Котка', 'Сиамска', '2022-03-15', 2);

INSERT INTO Veterinarians (FirstName, LastName, Specialty)
VALUES
('Николай', 'Иванов', 'Хирургия'),
('Елена', 'Тодорова', 'Терапия');

INSERT INTO Appointments (PetID, VetID, AppointmentDate, Diagnosis)
VALUES
(1, 1, '2026-10-10 10:00:00', 'Годишен преглед'),
(2, 2, '2026-10-11 14:30:00', 'Настинка');

INSERT INTO Treatments (AppointmentID, TreatmentName, Price)
VALUES
(1, 'Ваксинация', 40.00),
(2, 'Медикаментозно лечение', 25.00);


SELECT * FROM Owners;
SELECT * FROM Pets;
SELECT * FROM Veterinarians;
SELECT * FROM Appointments;
SELECT * FROM Treatments;