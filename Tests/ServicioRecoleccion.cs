using FireSharp;
using FireSharp.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tests.Clases;
using Tests.Models;

namespace Tests
{
    public partial class ServicioRecoleccion : Form
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
        GVIEntities db = new GVIEntities();
        DesarrollosGVIEntities db_des = new DesarrollosGVIEntities();
        #endregion
        public ServicioRecoleccion()
        {
            InitializeComponent();
        }

        #region Metodos_Privados
        private void inicioServicio(object sender, EventArgs e)
        {
            try
            {
                timer = new Timer();
                timer.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["Intervalo"]);
                timer.Enabled = true;
                this.timer.Tick += new EventHandler(EventoTemporizador);
                AgregarRegistro("Servicio iniciado con éxito");
                MessageBox.Show("Servicio iniciado", "Ok");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void AgregarRegistro(string mensaje)
        {
            try
            {
                // Agregar un registro al TextBox
                txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {mensaje}\r\n");

                // Desplazar el cursor al final del TextBox para mostrar el último mensaje
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void EventoTemporizador(object sender, EventArgs e)
        {
            try
            {
                var response = ProcesingData();

                if (response.Any())
                {
                    _client.Set("DataReference/", response);
                }

                AgregarRegistro($"{DateTime.Now}: La base de datos se sincronizó con éxito.\r\n");
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

        private void detenerServicio(object sender, EventArgs e)
        {
            try
            {
                timer.Enabled = false;
                timer.Stop();
                AgregarRegistro("Servicio detenido con éxito.");
                MessageBox.Show("Servicio detenido", "Ok");
            }
            catch (Exception ex)
            {
                AgregarRegistro($"Error al detener el servicio: {ex.Message}\r\n");
                MessageBox.Show(ex.Message,"Error");
            }
        }
        #endregion

        private void ServicioRecoleccion_Load(object sender, EventArgs e)
        {

        }
    }
}
