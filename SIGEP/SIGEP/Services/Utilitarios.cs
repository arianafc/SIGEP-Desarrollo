using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Services
{
    public class Utilitarios
    {
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
    }
}