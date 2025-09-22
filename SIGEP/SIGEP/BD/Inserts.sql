--INSERTS TABLA ESTADOS
INSERT INTO EstadosTB (Descripcion) VALUES ('Activo'), ('Inactivo')


--INSERTS TEMPORALES PARA ESPECIALIDADES
INSERT INTO EspecialidadesTB (Nombre, IdEstado) VALUES 
('Ejecutivo', 1),('Mecánica', 1), ('Informática', 1)

--INSERTS PARA ROLES
INSERT INTO RolesTB (Descripcion, IdEstado) VALUES ('Estudiante',1), ('Coordinador',1), ('Profesor',1), ('Egresado',1)
--INSERTS SECCIONES
INSERT INTO SeccionesTB (Seccion, IdEstado) VALUES ('12-1', 1), ('12-2', 1), ('12-3', 1), ('12-4', 1), ('N/A', 1)