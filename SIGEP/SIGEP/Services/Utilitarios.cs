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
       
        public bool EnviarCorreoConAdjunto(string destinatario, string mensaje, string asunto, string rutaAdjunto)
        {
            try
            {
                var remitente = ConfigurationManager.AppSettings["CorreoRemitente"];
                var contrasena = ConfigurationManager.AppSettings["CorreoPassword"];

                MailMessage mail = new MailMessage
                {
                    
                    From = new MailAddress(remitente),
                    Subject = asunto,
                    Body = mensaje,
                    IsBodyHtml = true
                };

                mail.To.Add(destinatario);

                if (!string.IsNullOrEmpty(rutaAdjunto) && System.IO.File.Exists(rutaAdjunto))
                    mail.Attachments.Add(new Attachment(rutaAdjunto));

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(remitente, contrasena),
                    EnableSsl = true
                };

                smtp.Send(mail);
                return true;
            }
            catch
            {
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

                // Para Office 365
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