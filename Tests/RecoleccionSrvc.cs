using FireSharp;
using FireSharp.Config;
using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Tests.Models;
using System.Linq;
using System.Collections.Generic;
using Tests.Clases;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Tests
{
    public partial class RecoleccionSrvc : ServiceBase
    {
        #region Variables
        private Timer timer;
        private string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log", "Log.txt");
        private static FirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = "aSIQGNiUjx68z1AWucwHqjw7Hko0YUMKGd4XxCFA",
            BasePath = "https://recoleccion-gvi-default-rtdb.firebaseio.com/"
        };

        private FirebaseClient _client = new FirebaseClient(config);
        private GVIEntities db = new GVIEntities();
        private DesarrollosGVIEntities db_des = new DesarrollosGVIEntities();
        private static readonly object lockObject = new object();
        #endregion

        public RecoleccionSrvc()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            IniciarServicio();
        }

        protected override void OnStop()
        {
            DetenerServicio();
        }

        #region Metodos_Privados

        private void IniciarServicio()
        {
            try
            {
                int intervalo = Convert.ToInt32(ConfigurationManager.AppSettings["Intervalo"]);

                // Cambiar Timer a System.Threading.Timer
                timer = new Timer(EventoTemporizador, null, 0, intervalo);

                AgregarRegistro("Servicio iniciado con éxito");
            }
            catch (Exception ex)
            {
                AgregarRegistro("Error: " + ex.Message);
            }
        }


        private void AgregarRegistro(string mensaje)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {mensaje}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void EventoTemporizador(object sender)
        {
            try
            {
                var response = ProcesingData();

                if (response.Any())
                {
                    _client.Set("DataReference/", response);
                }
            }
            catch (Exception ex)
            {
                AgregarRegistro($"{DateTime.Now}: Error al sincronizar la base de datos: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }

        private List<RE_TABLE_DATA> ProcesingData()
        {
            try
            {
                var response = db_des.RE_TABLE_DATA.ToList();
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new List<RE_TABLE_DATA>();
            }
        }

        private void DetenerServicio()
        {
            try
            {
                // Cambiar el tiempo de espera a Timeout.Infinite para deshabilitar futuras invocaciones
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                AgregarRegistro("Servicio detenido con éxito.");
            }
            catch (Exception ex)
            {
                AgregarRegistro($"Error al detener el servicio: {ex.Message}\r\n");
            }
        }
        #endregion
    }
}
