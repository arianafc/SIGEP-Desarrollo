--INSERTS TABLA ESTADOS
INSERT INTO EstadosTB (Descripcion) VALUES ('Activo'), ('Inactivo'), ('En proceso de Aplicacion'), ('Rechazada'), ('Asignada'), ('Aprobada'), ('Retirada'), ('Finalizada'), ('Rezagado'), ('Archivado'), ('En Curso')

--INSERTS TEMPORALES PARA ESPECIALIDADES
INSERT INTO EspecialidadesTB (Nombre, IdEstado) VALUES 
('Ejecutivo', 1),('Mecánica', 1), ('Informática', 1)

--INSERTS PARA ROLES
INSERT INTO RolesTB (Descripcion, IdEstado) VALUES ('Estudiante',1), ('Coordinador',1), ('Profesor',1), ('Egresado',1)
--INSERTS SECCIONES
INSERT INTO SeccionesTB (Seccion, IdEstado) VALUES ('12-1', 1), ('12-2', 1), ('12-3', 1), ('12-4', 1), ('N/A', 1)

USE [SIGEP]
GO

-- Insertar provincias
INSERT INTO ProvinciasTB (Nombre) VALUES 
('San José'),
('Alajuela'),
('Cartago'),
('Heredia'),
('Guanacaste'),
('Puntarenas'),
('Limón');

-- Insertar cantones de San José
INSERT INTO CantonesTB (Nombre, IdProvincia) VALUES 
('Central', 1),
('Escazú', 1),
('Desamparados', 1),
('Puriscal', 1),
('Tarrazú', 1),
('Aserrí', 1),
('Mora', 1),
('Goicoechea', 1),
('Santa Ana', 1),
('Alajuelita', 1);

-- Insertar cantones de Alajuela
INSERT INTO CantonesTB (Nombre, IdProvincia) VALUES 
('Central', 2),
('San Ramón', 2),
('Grecia', 2),
('San Mateo', 2),
('Atenas', 2),
('Naranjo', 2),
('Palmares', 2),
('Poás', 2),
('Orotina', 2),
('San Carlos', 2);

-- Insertar cantones de Cartago
INSERT INTO CantonesTB (Nombre, IdProvincia) VALUES 
('Central', 3),
('Paraíso', 3),
('La Unión', 3),
('Jiménez', 3),
('Turrialba', 3),
('Alvarado', 3),
('Oreamuno', 3),
('El Guarco', 3);

-- Insertar distritos de San José Central
INSERT INTO DistritosTB (Nombre, IdCanton) VALUES 
('Carmen', 1),
('Merced', 1),
('Hospital', 1),
('Catedral', 1),
('Zapote', 1),
('San Francisco De Dos Rios', 1),
('Uruca', 1),
('Mata Redonda', 1),
('Pavas', 1),
('Hatillo', 1),
('San Sebastián', 1);

-- Insertar distritos de Escazú
INSERT INTO DistritosTB (Nombre, IdCanton) VALUES 
('Escazú', 2),
('San Antonio', 2),
('San Rafael', 2);

-- Insertar distritos de Desamparados
INSERT INTO DistritosTB (Nombre, IdCanton) VALUES 
('Desamparados', 3),
('San Miguel', 3),
('San Juan De Dios', 3),
('San Rafael Arriba', 3),
('San Rafael Abajo', 3);

-- Insertar algunos distritos de Alajuela Central
INSERT INTO DistritosTB (Nombre, IdCanton) VALUES 
('Alajuela', 12),
('San José', 12),
('Carrizal', 12),
('San Antonio', 12);

-- Insertar algunos distritos de Cartago Central
INSERT INTO DistritosTB (Nombre, IdCanton) VALUES 
('Oriental', 21),
('Occidental', 21),
('Carmen', 21);
