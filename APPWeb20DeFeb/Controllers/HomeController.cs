using APPWeb20DeFeb.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;


namespace APPWeb20DeFeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        [HttpPost]
        public ActionResult Login(String Usuario, String Contraseña)
        {
            try
            {



                using (Models.escuelaEntities2 db = new Models.escuelaEntities2())
                {
                    var oUser = (from a in db.Maestros
                                 where a.NumeroControl == Usuario.Trim() && a.Contraseña == Contraseña.Trim()
                                 select a).FirstOrDefault();
                    if (oUser == null)
                    {
                        ViewBag.Error = "error";
                        return View();
                    }

                    Session["user"] = User;
                    GetID(Usuario, Contraseña);
                    GetData();


                }
            }
            catch (Exception ex)
            {

            }
            //return RedirectToAction("Index","Home");
            //return View("PasarListaPrincipal");
            return View("Menu");
        }

        public ActionResult GetData()
        {
            var ID = Session["ID"] as int?;
            try
            {
                using (Models.escuelaEntities2 db = new Models.escuelaEntities2())
                {
                    var listaSalones = db.Grupos
                                         .Where(a => a.MaestroFk == ID)  // Filtra por el maestro con ID = 1
                                         .Select(a => new { a.ID, a.Nombre })
                                         .ToList();

                    // Verificar que haya datos antes de pasarlos a la vista
                    if (listaSalones.Any())
                    {
                        ViewBag.Salones = new SelectList(listaSalones, "ID", "Nombre");
                    }
                    else
                    {
                        ViewBag.Salones = new SelectList(new List<object>(), "ID", "Nombre");
                    }
                }

            }
            catch (Exception ex) { }
            return View();

        }
        public ActionResult GetData1()
        {
            String salon = Session["salon"] as string;


            try
            {
                using (Models.escuelaEntities2 db = new Models.escuelaEntities2())
                {
                    // Obtener la lista de alumnos del grupo "1o A"
                    var listaPases = db.Alumnos
                                      .Where(a => a.Grupo == salon)
                                      .ToList();

                    ViewBag.ListaPase = listaPases;


                    if (Session["IDLista"] == null)
                    {
                        Session["IDLista"] = new List<int>();
                    }
                    List<int> IDLista = Session["IDLista"] as List<int>;

                    IDLista = listaPases.Select(a => a.ID).ToList(); ;
                    Session["IDLista"] = IDLista;
                }
            }
            catch (Exception ex)
            {

                ViewBag.Error = "Ocurrió un error al obtener los datos.";
            }

            return View();
        }






        [HttpPost]
        public ActionResult PasarALista(string salonSeleccionado)
        {

            Session["salon"] = salonSeleccionado;
            GetData1();
            return View("PasarListaPrincipal");


        }

        public ActionResult ViewImprimir()
        {
            GetData();
            return View("Imprimir");
        }


        //aqui empiezan las funciones para el pdf
        public List<PDFAlumnosData> GetDataPDF(string fecha, string grupo_)
        {
            string query = "select * from AlumnosAsistencia where fecha='" + fecha + "' AND Grupo='" + grupo_ + "';";
            var _EnviarPDFList = new List<PDFAlumnosData>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var PDF_ = new PDFAlumnosData
                        {
                            NumeroControl_ = reader.GetString(0),
                            Asistencia_ = reader.GetString(1),
                            Fecha_ = reader.GetDateTime(2)
                        };
                        _EnviarPDFList.Add(PDF_);
                    }
                
                }
            }
            return _EnviarPDFList;
        }
        
        public ActionResult Descargar()
        {
            // Leer los datos de TempData
            var alumnos = TempData["Alumnos"] as List<PDFAlumnosData>;

            if (alumnos == null || alumnos.Count == 0)
            {
                return Content("No hay datos para generar el PDF.");
            }

     
            using (MemoryStream memoryStream = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(memoryStream);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Crear fuente con iText 7
                PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Agregar título
                document.Add(new Paragraph("Lista de Asistencia")
                    .SetFont(font)
                    .SetFontSize(12));

                document.Add(new Paragraph($"Fecha: {alumnos[0].Fecha_.ToString("yyyy-MM-dd")}\n\n"));

                // Crear tabla con 3 columnas
                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 3, 3 })).UseAllAvailableWidth();

                // Agregar encabezados
                table.AddHeaderCell("Número de Control");
                table.AddHeaderCell("Asistencia");
                table.AddHeaderCell("Fecha");

                // Agregar datos
                foreach (var alumno in alumnos)
                {
                    table.AddCell(alumno.NumeroControl_);
                    table.AddCell(alumno.Asistencia_);
                    table.AddCell(alumno.Fecha_.ToString("yyyy-MM-dd"));
                }

                document.Add(table);
                document.Close();

          
                byte[] pdfBytes = memoryStream.ToArray();

                return File(pdfBytes, "application/pdf", "Asistencia.pdf");
            }
        }

        [HttpPost]
        public ActionResult ImprimirPDF(string salonSeleccionadoIm, string FechaSeleccionadoIm)
        {
            if (!string.IsNullOrEmpty(salonSeleccionadoIm) && !string.IsNullOrEmpty(FechaSeleccionadoIm))
            {
                // Obtener los datos de los alumnos desde la base de datos
                var alumnos = GetDataPDF(FechaSeleccionadoIm, salonSeleccionadoIm);

                // Verificar si se encontraron alumnos
                if (alumnos != null && alumnos.Count > 0)
                {
                    TempData["Alumnos"] = alumnos;

                
                    // Redirigir al método DescargarPDF
                    return RedirectToAction("Descargar");
                }
                else
                {
                    // En caso de que no se encuentren alumnos
                    return Content("No se encontraron datos para generar el PDF.");
                }
            }
            else
            {
                return Content("Error al seleccionar los datos, intente de nuevo.");
            }
        }


      









        [HttpPost]
        public ActionResult KeepList(List<string> seleccionados, List<string> Nseleccionados)
        {
            string salon = Session["salon"] as string;

            List<Models.AlumnosAsistencia> asistenciaLista = new List<Models.AlumnosAsistencia>();

            // Agregar los seleccionados con asistencia "1"
            if (seleccionados != null && seleccionados.Count > 0)
            {
                asistenciaLista.AddRange(seleccionados.Select(a => new Models.AlumnosAsistencia
                {
                    NumeroControl = a,
                    Asistencia = "1",  // Presente
                    fecha = DateTime.Now,
                    Grupo = salon
                }));
            }

            // Agregar los no seleccionados con asistencia "0"
            if (Nseleccionados != null && Nseleccionados.Count > 0)
            {
                asistenciaLista.AddRange(Nseleccionados.Select(a => new Models.AlumnosAsistencia
                {
                    NumeroControl = a,
                    Asistencia = "0",  // Ausente
                    fecha = DateTime.Now,
                    Grupo = salon
                }));
            }

            // Insertar en la base de datos
            if (asistenciaLista.Count > 0)
            {
                InsertarAlumnos(asistenciaLista);
                return Content($"Asistencia guardada. Presentes: {string.Join(", ", seleccionados ?? new List<string>())}. Ausentes: {string.Join(", ", Nseleccionados ?? new List<string>())}.");
            }

            return Content("No se han registrado asistencias.");
        }


        string connectionString = "xxxxxxxxxxxxxxxxxxx";

        public void InsertarAlumnos(List<Models.AlumnosAsistencia> alumnos)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Definir la consulta SQL
                string query = "INSERT INTO AlumnosAsistencia (NumeroControl, Asistencia, fecha, Grupo) VALUES (@NumeroControl, @Asistencia, @fecha, @Grupo)";

                foreach (var alumno in alumnos)
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Agregar parámetros
                        command.Parameters.AddWithValue("@NumeroControl", alumno.NumeroControl);
                        command.Parameters.AddWithValue("@Asistencia", alumno.Asistencia);  // Asegúrate que la propiedad sea 'Asistencia'
                        command.Parameters.AddWithValue("@fecha", alumno.fecha);  // Asegúrate que la propiedad sea 'fecha'
                        command.Parameters.AddWithValue("@Grupo", alumno.Grupo);  // Asegúrate que la propiedad sea 'fecha'

                        // Ejecutar la inserción
                        command.ExecuteNonQuery();
                    }
                }
            }
        }


       







        public void GetID(String Usuario, String Contraseña)
        {
            using (Models.escuelaEntities2 db = new Models.escuelaEntities2())
            {
                var oUser = (from a in db.Maestros
                             where a.NumeroControl == Usuario.Trim() && a.Contraseña == Contraseña.Trim()
                             select a).FirstOrDefault();

                int userId = oUser != null ? oUser.ID : 0; // Suponiendo que 'Id' es la clave primaria



                Session["ID"] = userId;



            }
        }

       
   
    


    }
}



public class PDFAlumnosData
{
    public String NumeroControl_ { get; set; }
    public string Asistencia_ { get; set; }
    public DateTime Fecha_ { get; set; }
}