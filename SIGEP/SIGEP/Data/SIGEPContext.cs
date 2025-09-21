using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace SIGEP.Models
{
    public class SIGEPContext : DbContext
    {
        
        public SIGEPContext() : base("name=Sigep")
        {
        }

        // Tablas
        public DbSet<AuditoriaGlobal> AuditoriasGlobales { get; set; }
        public DbSet<BolsaEmpleo> BolsasEmpleo { get; set; }
        public DbSet<Canton> Cantones { get; set; }
        public DbSet<ComentarioPractica> ComentariosPractica { get; set; }
        public DbSet<Comunicado> Comunicados { get; set; }
        public DbSet<Direccion> Direcciones { get; set; }
        public DbSet<Distrito> Distritos { get; set; }
        public DbSet<Documento> Documentos { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<Empleo> Empleos { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Encargado> Encargados { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<EspecialidadVacante> EspecialidadesVacantes { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<EstudianteEncargado> EstudiantesEncargados { get; set; }
        public DbSet<FormacionAcademica> FormacionesAcademicas { get; set; }
        public DbSet<InformacionLaboral> InformacionesLaborales { get; set; }
        public DbSet<InformacionMedica> InformacionesMedicas { get; set; }
        public DbSet<Modalidad> Modalidades { get; set; }
        public DbSet<PracticaEstudiante> PracticasEstudiantes { get; set; }
        public DbSet<Provincia> Provincias { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Seccion> Secciones { get; set; }
        public DbSet<Telefono> Telefonos { get; set; }
        public DbSet<UsuarioEspecialidad> UsuariosEspecialidades { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<VacantePractica> Vacantes { get; set; }
       
    }
}