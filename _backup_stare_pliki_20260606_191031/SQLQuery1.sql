USE SeaSharkDB;
GO

-- Zmieniamy role zgodnie z nowym planem
UPDATE Uzytkownicy SET Rola = 'Technolog' WHERE Login = 'michal';
UPDATE Uzytkownicy SET Rola = 'Handlowiec' WHERE Login = 'dawid';
UPDATE Uzytkownicy SET Rola = 'Admin' WHERE Login = 'admin';

-- Sprawdzamy, czy wszystko się zgadza
SELECT * FROM Uzytkownicy;