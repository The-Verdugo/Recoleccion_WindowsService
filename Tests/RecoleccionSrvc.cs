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
        private async void IniciarServicio()
        {
            try
            {
                string logDirectory = Path.GetDirectoryName(logFilePath);

                // Verificar si la carpeta existe, y si no, crearla
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                int intervalo = Convert.ToInt32(ConfigurationManager.AppSettings["Intervalo"]);

                while (true)
                {
                    // Espera el intervalo de tiempo
                    await Task.Delay(intervalo);

                    // Ejecuta el código de la tarea
                    EjecutarTarea(null);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: Ocurrió un error al iniciar el servicio: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }

        private async void EjecutarTarea(object state)
        {
            try
            {
                var response = await Task.Run(() => ProcesingData());

                if (response.version > 0)
                {
                    _client.Set("DatabaseVersion/", response);
                }

                File.AppendAllText(logFilePath, $"{DateTime.Now}: La base se sincronizó con éxito.\r\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: Error al sincronizar la base de datos: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }

        private async Task<ResponseVersion> ProcesingData()
        {
            try
            {
                bool hayCambios = false;
                ResponseVersion response = new ResponseVersion();

                var version = await db_des.Re_Data_Reference
                    .Select(c => new { c.version_data, c.fecha_version })
                    .FirstOrDefaultAsync();

                if (version == null || version.fecha_version == null || version.fecha_version.Value.Date != DateTime.Today)
                {
                    var existingKeys = await db_des.Re_Data_Reference.Select(r => r.DocEntry).ToListAsync();

                    var sourceDataList = await db.fn_API_Get_DataSource()
                        .Select(source => new Re_Data_Reference
                        {
                            id = 0,
                            DocEntry = source.DocEntry,
                            // Resto de las propiedades...
                            version_data = 1,
                            fecha_version = DateTime.Today
                        })
                        .ToListAsync();

                    var keysToDelete = existingKeys.Except(sourceDataList.Select(r => r.DocEntry)).ToList();
                    var recordsToDelete = await db_des.Re_Data_Reference.Where(r => keysToDelete.Contains(r.DocEntry)).ToListAsync();
                    db_des.Re_Data_Reference.RemoveRange(recordsToDelete);

                    var existingRecordsToUpdate = await db_des.Re_Data_Reference.Where(r => existingKeys.Contains(r.DocEntry)).ToListAsync();
                    foreach (var record in existingRecordsToUpdate)
                    {
                        record.version_data = 1;
                    }

                    var recordsToInsert = sourceDataList.Where(r => !existingKeys.Contains(r.DocEntry)).ToList();
                    db_des.Re_Data_Reference.AddRange(recordsToInsert);
                    await db_des.SaveChangesAsync();

                    hayCambios = keysToDelete.Any() || recordsToInsert.Any() || existingRecordsToUpdate.Any();

                    response.fecha = DateTime.Today.ToString("dd-MM-yyyy");
                    response.version = 1;
                }
                else
                {
                    var dataFromQuery = await db.fn_API_Get_DataSource().ToListAsync();
                    var dataTableOfSources = await db_des.Re_Data_Reference.ToListAsync();

                    var changes = dataFromQuery
                        .Join(dataTableOfSources, x => x.DocEntry, y => y.DocEntry, (x, y) => new { Source = x, Destination = y })
                        .Where(pair => pair.Destination != null &&
                                        (pair.Source.Valida_Picking != pair.Destination.Valida_Picking ||
                                         pair.Source.Paqueteria != pair.Destination.Paqueteria ||
                                         pair.Source.Rotulo != pair.Destination.Rotulo))
                        .ToList();

                    var elementsToAdd = dataFromQuery
                        .Where(source => !dataTableOfSources.Any(destination => destination.DocEntry == source.DocEntry))
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
                            var existingRecord = await db_des.Re_Data_Reference.FirstOrDefaultAsync(r => r.DocEntry == change.Destination.DocEntry);
                            if (existingRecord != null)
                            {
                                existingRecord.Valida_Picking = change.Source.Valida_Picking;
                                existingRecord.Paqueteria = change.Source.Paqueteria;
                                existingRecord.Prioridad = change.Source.Prioridad;
                                existingRecord.Rotulo = change.Source.Rotulo;
                                existingRecord.version_data = nuevaVersion;
                                existingRecord.fecha_version = DateTime.Today;
                            }
                        }

                        db_des.Re_Data_Reference.AddRange(elementsToAdd.Select(source => new Re_Data_Reference
                        {
                            id = 0,
                            DocEntry = source.DocEntry,
                            // Resto de las propiedades...
                            version_data = nuevaVersion,
                            fecha_version = DateTime.Today
                        }));

                        db_des.Re_Data_Reference.RemoveRange(elementsToDelete);
                        await db_des.SaveChangesAsync();

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new ResponseVersion();
            }
        }

        private void DetenerServicio()
        {
            try
            {
                timer?.Dispose();
                File.AppendAllText(logFilePath, $"{DateTime.Now}: La tarea se ha detenido con éxito.\r\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: Ocurrió un error al detener el servicio: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }
        #endregion
    }
}
