using FireSharp;
using FireSharp.Config;
using System;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
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
                var response = ProcesingData(); // Esperar la tarea

                if (response.version > 0)
                {
                    _client.Set("DatabaseVersion/", response);
                }

                AgregarRegistro($"{DateTime.Now}: La base de datos se sincronizó con éxito.\r\n");
            }
            catch (Exception ex)
            {
                AgregarRegistro($"{DateTime.Now}: Error al sincronizar la base de datos: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }

       private ResponseVersion ProcesingData()
        {
            try
            {
                bool hayCambios = false;
                ResponseVersion response = new ResponseVersion();
                using (var db_des = new DesarrollosGVIEntities())
                {
                    var version = db_des.Re_Data_Reference
                    .Select(c => new { c.version_data, c.fecha_version })
                    .FirstOrDefault();

                    if (version == null || version.fecha_version == null || version.fecha_version.Value.Date != DateTime.Today)
                    {
                        var existingKeys = db_des.Re_Data_Reference.Select(r => r.DocEntry).ToList();
                 
                        var sourceDataList = db.fn_API_Get_DataRecoleccion()
                            .Select(source => new Re_Data_Reference
                            {
                                id = 0,
                                DocEntry = source.DocEntry,
                                CreateDate = source.CreateDate,
                                DesAlmDes = source.DesAlmDes,
                                Factura = source.Factura,
                                Paqueteria = source.Paqueteria,
                                Prioridad = source.Prioridad,
                                Rotulo = source.Rotulo,
                                U_CardName = source.U_CardName,
                                Valida_Picking = source.Valida_Picking,
                                Valida_Packing = source.Valida_Packing,
                                version_data = 1,
                                fecha_version = DateTime.Today
                            })
                            .ToList();

                        var keysToDelete = existingKeys.Except(sourceDataList.Select(r => r.DocEntry)).ToList();
                        var recordsToDelete = db_des.Re_Data_Reference.Where(r => keysToDelete.Contains(r.DocEntry)).ToList();
                        db_des.Re_Data_Reference.RemoveRange(recordsToDelete);

                        var existingRecordsToUpdate = db_des.Re_Data_Reference.Where(r => existingKeys.Contains(r.DocEntry)).ToList();
                        foreach (var record in existingRecordsToUpdate)
                        {
                            record.fecha_version = DateTime.Today;
                            record.version_data = 1;
                        }

                        var recordsToInsert = sourceDataList.Where(r => !existingKeys.Contains(r.DocEntry)).ToList();

                        db_des.Re_Data_Reference.AddRange(recordsToInsert);

                        db_des.SaveChanges();

                        hayCambios = keysToDelete.Any() || recordsToInsert.Any() || existingRecordsToUpdate.Any();

                        response.fecha = DateTime.Today.ToString("dd-MM-yyyy");
                        response.version = 1;
                    }
                    else
                    {

                        // Obtener los DocEntry duplicados
                        var duplicatedDocEntries = db_des.Re_Data_Reference
                            .GroupBy(entry => entry.DocEntry)
                            .Where(group => group.Count() > 1)
                            .Select(group => group.Key)
                            .ToList();

                        // Eliminar registros duplicados dejando solo el más reciente
                        var recordsToDelete = db_des.Re_Data_Reference
                            .Where(entry => duplicatedDocEntries.Contains(entry.DocEntry))
                            .GroupBy(entry => entry.DocEntry)
                            .SelectMany(group => group.OrderByDescending(entry => entry.version_data).Skip(1)) // Omitir el más reciente
                            .ToList();

                        db_des.Re_Data_Reference.RemoveRange(recordsToDelete);
                        db_des.SaveChanges();

                        var dataFromQuery = db_des.RE_TABLE_DATA.ToList();
                        var dataTableOfSources = db_des.Re_Data_Reference.ToList();

                        var changes = dataFromQuery
                            .Join(dataTableOfSources, x => x.DocEntry, y => y.DocEntry, (x, y) => new { Source = x, Destination = y })
                            .Where(pair => pair.Destination != null &&
                                            (pair.Source.Valida_Picking != pair.Destination.Valida_Picking ||
                                             pair.Source.Valida_Packing != pair.Destination.Valida_Packing ||
                                             pair.Source.Paqueteria != pair.Destination.Paqueteria ||
                                             pair.Source.Rotulo != pair.Destination.Rotulo))
                            .ToList();

                        var elementsToAdd = dataFromQuery
                            .Where(source => !dataTableOfSources.Any(destination => destination.DocEntry == source.DocEntry))
                            .Select(source => new Re_Data_Reference
                            {
                                id = 0,
                                DocEntry = source.DocEntry,
                                CreateDate = source.CreateDate,
                                DesAlmDes = source.DesAlmDes,
                                Factura = source.Factura,
                                Paqueteria = source.Paqueteria,
                                Prioridad = source.Prioridad,
                                Rotulo = source.Rotulo,
                                U_CardName = source.U_CardName,
                                Valida_Picking = source.Valida_Picking,
                                Valida_Packing = source.Valida_Packing,
                                version_data = (int)(version.version_data + 1),
                                fecha_version = DateTime.Today
                            })
                            .ToList();

                        var elementsToDelete = dataTableOfSources
                        .Where(destination => !dataFromQuery.Any(source => source.DocEntry == destination.DocEntry))
                        .ToList();

                        hayCambios = changes.Any() || elementsToAdd.Any() || elementsToDelete.Any();

                        if (hayCambios)
                        {
                            int nuevaVersion = (int)(version.version_data + 1);

                            var unchangedElements = dataTableOfSources.Except(changes.Select(change => change.Destination));
                            foreach (var unchangedElement in unchangedElements)
                            {
                                unchangedElement.version_data = nuevaVersion;
                                unchangedElement.fecha_version = DateTime.Today;
                            }

                            foreach (var change in changes)
                            {
                                var existingRecords = dataTableOfSources
                                    .Where(x => x.DocEntry == change.Destination.DocEntry)
                                    .OrderByDescending(x => x.version_data)
                                    .FirstOrDefault();

                                if (existingRecords != null)
                                {
                                    existingRecords.Valida_Picking = change.Source.Valida_Picking;
                                    existingRecords.Valida_Packing = change.Source.Valida_Packing;
                                    existingRecords.Paqueteria = change.Source.Paqueteria;
                                    existingRecords.Prioridad = change.Source.Prioridad;
                                    existingRecords.Rotulo = change.Source.Rotulo;
                                    existingRecords.version_data = nuevaVersion;
                                    existingRecords.fecha_version = DateTime.Today;
                                    // Marcar el objeto como modificado
                                    db_des.Entry(existingRecords).State = EntityState.Modified;
                                }
                            }

                            db_des.Re_Data_Reference.AddRange(elementsToAdd);
                            db_des.Re_Data_Reference.RemoveRange(elementsToDelete);

                            db_des.SaveChanges();

                            response.version = nuevaVersion;
                            response.fecha = DateTime.Today.ToString("dd-MM-yyyy");
                        }
                        else
                        {
                            response.version = version.version_data.Value;
                            response.fecha = version.fecha_version.Value.ToString("dd-MM-yyyy");
                        }
                    }

                    string mensaje = hayCambios ? "Se detectaron cambios." : "No hay cambios.";
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new ResponseVersion();
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
