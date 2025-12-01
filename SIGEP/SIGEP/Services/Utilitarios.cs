using SIGEP.EF;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace SIGEP.Services
{
    public class Utilitarios
    {

       public int ObtenerOCrearDireccion(
SIGEPEntities db,
string nombreProvincia,
string nombreCanton,
string nombreDistrito,
string direccionExacta,
int idDireccion // 0 o negativo si es NUEVA dirección
)
        {
            if (string.IsNullOrWhiteSpace(nombreProvincia))
                throw new ArgumentException("La provincia es requerida.");

            if (string.IsNullOrWhiteSpace(nombreCanton))
                throw new ArgumentException("El cantón es requerido.");

            if (string.IsNullOrWhiteSpace(nombreDistrito))
                throw new ArgumentException("El distrito es requerido.");

            if (string.IsNullOrWhiteSpace(direccionExacta))
                throw new ArgumentException("La dirección exacta es requerida.");

            // 1. Provincia
            var provincia = db.ProvinciasTB
                              .FirstOrDefault(p => p.Nombre == nombreProvincia);

            if (provincia == null)
            {
                provincia = new ProvinciasTB
                {
                    Nombre = nombreProvincia
                };
                db.ProvinciasTB.Add(provincia);
                db.SaveChanges();
            }

            // 2. Cantón
            var canton = db.CantonesTB
                           .FirstOrDefault(c => c.Nombre == nombreCanton
                                             && c.IdProvincia == provincia.IdProvincia);

            if (canton == null)
            {
                canton = new CantonesTB
                {
                    Nombre = nombreCanton,
                    IdProvincia = provincia.IdProvincia
                };
                db.CantonesTB.Add(canton);
                db.SaveChanges();
            }

            // 3. Distrito
            var distrito = db.DistritosTB
                             .FirstOrDefault(d => d.Nombre == nombreDistrito
                                               && d.IdCanton == canton.IdCanton);

            if (distrito == null)
            {
                distrito = new DistritosTB
                {
                    Nombre = nombreDistrito,
                    IdCanton = canton.IdCanton
                };
                db.DistritosTB.Add(distrito);
                db.SaveChanges();
            }

            DireccionesTB direccion = null;


            if (idDireccion > 0)
            {
                direccion = db.DireccionesTB
                              .FirstOrDefault(di => di.IdDireccion == idDireccion);

                if (direccion != null)
                {
                    direccion.DireccionExacta = direccionExacta;
                    direccion.IdDistrito = distrito.IdDistrito;

                    db.SaveChanges();
                    return direccion.IdDireccion;
                }
            }


            direccion = new DireccionesTB
            {
                IdDistrito = distrito.IdDistrito,
                DireccionExacta = direccionExacta,
                IdEstado = 1
            };

            db.DireccionesTB.Add(direccion);
            db.SaveChanges();

            return direccion.IdDireccion;
        }




        public bool EnviarCorreoConAdjuntos(string destinatario, string mensaje, string asunto, List<string> rutasAdjuntos)
        {
            try
            {
                var remitente = ConfigurationManager.AppSettings["CorreoRemitente"];
                var contrasena = ConfigurationManager.AppSettings["CorreoPassword"];

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(remitente);
                    mail.To.Add(destinatario);
                    mail.Subject = asunto;
                    mail.Body = mensaje;
                    mail.IsBodyHtml = true;

                    
                    foreach (var ruta in rutasAdjuntos)
                    {
                        if (System.IO.File.Exists(ruta))
                        {
                            Attachment adjunto = new Attachment(ruta);
                            mail.Attachments.Add(adjunto);
                        }
                    }

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential(remitente, contrasena);
                        smtp.EnableSsl = true;

                        smtp.Send(mail); 
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"Error al enviar correo: {ex.Message}");
                return false;
            }
        }


        public bool EnviarCorreo(string destinatario, string mensaje, string asunto)
        {
            try
            {
                var remitente = ConfigurationManager.AppSettings["CorreoRemitente"];
                var contrasena = ConfigurationManager.AppSettings["CorreoPassword"];

                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(remitente),
                    To = { destinatario },
                    Subject = asunto,
                    Body = mensaje,
                    IsBodyHtml = true,
                };

             
                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(remitente, contrasena),
                    EnableSsl = true
                };

                smtp.Send(mail);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GenerarPassword(int longitud = 8)
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var sb = new StringBuilder(longitud);

            for (int i = 0; i < longitud; i++)
            {
                int index = random.Next(caracteres.Length);
                sb.Append(caracteres[index]);
            }

            return sb.ToString();
        }

        public string ObtenerMensajeSQL(Exception ex)
        {
            while (ex != null)
            {
                if (ex is System.Data.SqlClient.SqlException sqlEx)
                {
                    return sqlEx.Message;
                }
                ex = ex.InnerException;
            }
            return null;
        }

        public string GenerarPlantillaCorreo(string titulo, string contenido)
        {
            string ruta = HttpContext.Current.Server.MapPath("~/Plantillas/PlantillaComunicados.html");
            string html = File.ReadAllText(ruta);

            html = html.Replace("{TITULO}", titulo);
            html = html.Replace("{CONTENIDO}", contenido);

            return html;
        }

    }
}