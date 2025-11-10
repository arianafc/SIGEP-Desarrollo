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