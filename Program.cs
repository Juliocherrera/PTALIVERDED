using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Services;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using TLIVERDED.Models;
using UploadFile = TLIVERDED.Models.UploadFile;

namespace TLIVERDED
{
    public class Program
    {

        static storedProcedure sql = new storedProcedure("miConexion");
        public static FacLabControler facLabControler = new FacLabControler();
        public static string jsonFactura = "", idSucursal = "", idTipoFactura = "", IdApiEmpresa = "";
        public string leg;
        public static List<string> result = new List<string>();
        static string Fecha;
        static string Subtotal;
        static string Totalimptrasl;
        static string Totalimpreten;
        static string Descuentos;
        static string Total;
        static string FormaPago;
        static string Condipago;
        static string MetodoPago;
        static string Moneda;
        static string RFC;
        static string CodSAT;
        static string IdProducto;
        static string Producto;
        static string Origen;
        static string Destino;

        public static List<string> results = new List<string>();
        static HtmlTable table = new HtmlTable();

        static char[] caracter = { '|' };
        static string[] words;
        public static void Main(string[] args)
        {


            Program muobject = new Program();

            //PASO 1 - VALIDA EN TRALIX QUE NO EXISTA EL SEGMENTO
                DataTable td = facLabControler.GetLeg();
                if (td.Rows.Count > 0)
                {

                    foreach (DataRow item2 in td.Rows)
                    {

                        string folio = item2["segmento"].ToString();
                        var request2819 = (HttpWebRequest)WebRequest.Create("https://canal1.xsa.com.mx:9050/bf2e1036-ba47-49a0-8cd9-e04b36d5afd4/cfdis?folioEspecifico=" + folio);
                        var response2819 = (HttpWebResponse)request2819.GetResponse();
                        var responseString2819 = new StreamReader(response2819.GetResponseStream()).ReadToEnd();

                        List<ModelFact> separados819 = JsonConvert.DeserializeObject<List<ModelFact>>(responseString2819);
                    //PASO 2 - SI EXISTE LE ACTUALIZA EL ESTATUS A 9
                        if (separados819 != null)
                        {
                            foreach (var rlist in separados819)
                            {
                                string serie = rlist.serie;
                                if (serie == "TDRXP" || serie =="TDRZP")
                                {
                                    string tipomensaje = "9";
                                    DataTable updateLegs = facLabControler.UpdateLeg(folio, tipomensaje);
                                }
                                else
                                {
                                    DataTable res = facLabControler.GetSegmentoRepetido(folio);
                                    //PASO 4 - SI EXISTE LE ACTUALIZA EL ESTATUS A 9
                                    if (res.Rows.Count > 0)
                                    {
                                        string foliorepetido = item2["segmento"].ToString();
                                        Console.WriteLine("El Folio ya esta timbrado" + foliorepetido);

                                        string tipom = "9";
                                        DataTable updateLeg = facLabControler.UpdateLeg(foliorepetido, tipom);
                                        foreach (DataRow item3 in updateLeg.Rows)
                                        {
                                            string rupdate = item3["segmento"].ToString();
                                            string lupdate = item3["estatus"].ToString();
                                        }
                                    }
                                    else  // PASO 5 - SI NO EXISTE CONTINUA CON EL PROCESO DE TIMBRADO
                                    {
                                        DataTable results = facLabControler.TieneMercancias(folio);
                                        //PASO 4 - SI EXISTE LE ACTUALIZA EL ESTATUS A 9
                                        foreach (DataRow resl in results.Rows)
                                        {
                                            string totald = resl["total"].ToString();
                                            int num_var = Int32.Parse(totald);
                                            if (num_var > 0)
                                            {
                                                foreach (DataRow item in td.Rows)
                                                {
                                                    string legs = item["segmento"].ToString();

                                                    valida(legs);
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                           
                        }
                        else
                        {
                        //PASO 3 - VALIDA QUE NO ESTE REGISTRADO EN LA VISTA_CARTA_PORTE
                            DataTable res = facLabControler.GetSegmentoRepetido(folio);
                        //PASO 4 - SI EXISTE LE ACTUALIZA EL ESTATUS A 9
                        if (res.Rows.Count > 0)
                            {
                                string foliorepetido = item2["segmento"].ToString();
                                Console.WriteLine("El Folio ya esta timbrado" + foliorepetido);

                                string tipom = "9";
                                DataTable updateLeg = facLabControler.UpdateLeg(foliorepetido, tipom);
                                foreach (DataRow item3 in updateLeg.Rows)
                                {
                                    string rupdate = item3["segmento"].ToString();
                                    string lupdate = item3["estatus"].ToString();
                                }
                            }
                            else  // PASO 5 - SI NO EXISTE CONTINUA CON EL PROCESO DE TIMBRADO
                            {
                                DataTable results = facLabControler.TieneMercancias(folio);
                            //PASO 4 - SI EXISTE LE ACTUALIZA EL ESTATUS A 9
                                foreach (DataRow resl in results.Rows)
                                {
                                    string totald = resl["total"].ToString();
                                    int num_var = Int32.Parse(totald);
                                    if (num_var > 0)
                                    {
                                        foreach (DataRow item in td.Rows)
                                        {
                                            string legs = item["segmento"].ToString();

                                            valida(legs);
                                        }
                                    }
                                }
                            }

                        }
                        
                    }
                }
        }

        public static List<string> valida(string leg)
        {
            string compCarta = "";
            results.Clear();
            //PASO 6 - VALIDA EL TAMAÑO DEL SEGMENTO
            if (leg.Length > 0 && leg != "null" && leg != "")
            {
                try
                {
                    //VALIDO QUE TENGA MERCANCIA

                    List<string> validaCFDI = new List<string>();
                    //PASO 7 - VALIDA QUE ESTE OK LA CARTAPORTE
                    validaCFDI = sql.recuperaRegistros("exec sp_validaCFDICartaporte " + leg);
                    if (validaCFDI.Count > 0)
                    {
                        //PASO 8 - VALIDA QUE ESTE OK EL RESULTADO
                        if (validaCFDI[1].Contains("OK"))
                        {
                            //PASO 9 - CREA EL CUERPO DEL TXT
                            compCarta = sql.recuperaValor("exec sp_compCartaPorte " + leg);
                            if (compCarta.Length > 0)
                            {
                                tiposCfds();
                                words = Regex.Replace(compCarta, @"\r\n?|\n", "").Split('|');
                                iniciaDatos();
                                //PASO 10 - INGRESA PARA TIMBRAR LA CARTAPORTE
                                if (Cartaporte(leg, compCarta))
                                {
                                    //PASO 14 - ACTUALIZA EL ESTATUS A 2 - OK 
                                    results.Add("ok");//mostrar  }
                                    string tipom = "2";
                                    string mensaje = "Cartaporte timbrada con exito!!!";
                                    DataTable updateLeg = facLabControler.UpdateLeg(leg, tipom);
                                    foreach (DataRow item in updateLeg.Rows)
                                    {
                                        string rupdate = item["segmento"].ToString();
                                        string lupdate = item["estatus"].ToString();
                                    }
                                    facLabControler.enviarNotificacion(leg, mensaje);

                                    //Aqui actualizamos en estatus 

                                }
                                else
                                {
                                    results.Clear();
                                    results.Add("Error1");
                                    results.Add("Ver el historial de errores para mas información, copiar el error y reportar a TI.");
                                    string tipom = "3";
                                    string mensaje = "Ver el historial de errores para mas información, copiar el error y reportar a TI.";
                                    DataTable updateLeg = facLabControler.UpdateLeg(leg, tipom);
                                    facLabControler.enviarNotificacion(leg,mensaje);
                                }
                            }
                            else
                            {
                                results.Clear();
                                results.Add("Error1");
                                results.Add("Error al generar carta porte.");//mostrar 
                                string tipom = "3";
                                string mensaje = "Error al generar carta porte.";
                                DataTable updateLeg = facLabControler.UpdateLeg(leg, tipom);
                                facLabControler.enviarNotificacion(leg,mensaje);
                            }
                        }
                        else
                        {
                            // ERROR: YA EXISTE O YA ESTA TIMBRADO
                            results.Clear();
                            results.Add("Error");
                            results.Add("Error en la obtención de datos: \r\n" + validaCFDI[0]);//mostrar 
                            string tipom = "5";
                            string mensaje = "Error en la obtención de datos:";
                            DataTable updateLeg = facLabControler.UpdateLeg(leg, tipom);
                            facLabControler.enviarNotificacion(leg,mensaje);
                        }
                    }
                    else
                    {
                        results.Clear();
                        results.Add("Error");
                        results.Add("Error al validar el segmento.");//mostrar 
                        string tipom = "3";
                        string mensaje = "Error al validar el segmento.";
                        DataTable updateLeg = facLabControler.UpdateLeg(leg, tipom);
                        facLabControler.enviarNotificacion(leg,mensaje);
                    }
                }
                catch (Exception)
                {
                    results.Clear();
                    results.Add("Error");
                    results.Add("Segmento invalido");
                    string tipom = "3";
                    string mensaje = "Segmento invalido";
                    DataTable updateLeg = facLabControler.UpdateLeg(leg, tipom);
                    facLabControler.enviarNotificacion(leg,mensaje);
                }
            }
            else { results.Add("Error3"); }
            return results;
        }
        //Esto es lo nuevo

        //[WebMethod]
        //public static List<string> valida2(string leg)
        //{
        //    string compCarta = "";
        //    result.Clear();




        //    try
        //    {
        //        List<string> validaCFDI = new List<string>();
        //        validaCFDI = sql.recuperaRegistros("exec sp_validaCFDICartaporte " + leg);
        //        if (validaCFDI.Count > 0)
        //        {
        //            if (validaCFDI[1].Contains("OK"))
        //            {
        //                compCarta = sql.recuperaValor("exec sp_compCartaPorte " + leg);
        //                if (compCarta.Length > 0)
        //                {
        //                    tiposCfds();
        //                    words = Regex.Replace(compCarta, @"\r\n?|\n", "").Split('|');
        //                    iniciaDatos();
        //                    if (Cartaporte(leg, compCarta))
        //                    {
        //                        result.Add("ok");//mostrar  }
        //                    }
        //                    else
        //                    {
        //                        result.Clear();
        //                        result.Add("Error1");
        //                        result.Add("Ver el historial de errores para mas información, copiar el error y reportar a TI.");
        //                    }
        //                }
        //                else
        //                {
        //                    result.Clear();
        //                    result.Add("Error1");
        //                    result.Add("Error al generar carta porte.");//mostrar 
        //                }
        //            }
        //            else
        //            {
        //                result.Clear();
        //                result.Add("Error");
        //                result.Add("Error en la obtención de datos: \r\n" + validaCFDI[0]);//mostrar 
        //            }
        //        }
        //        else
        //        {
        //            result.Clear();
        //            result.Add("Error");
        //            result.Add("Error al validar el segmento.");//mostrar 
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        result.Clear();
        //        result.Add("Error");
        //        result.Add("Factura invalida");
        //    }

        //    return result;
        //}

        public static void tiposCfds()
        {
            var request_ = (HttpWebRequest)WebRequest.Create("https://canal1.xsa.com.mx:9050/" + "bf2e1036-ba47-49a0-8cd9-e04b36d5afd4" + "/tiposCfds");
            var response_ = (HttpWebResponse)request_.GetResponse();
            var responseString_ = new StreamReader(response_.GetResponseStream()).ReadToEnd();

            string[] separadas_ = responseString_.Split('}');

            foreach (string dato in separadas_)
            {
                if (dato.Contains("TDRXP"))
                {
                    string[] separadasSucursal_ = dato.Split(',');
                    foreach (string datoSuc in separadasSucursal_)
                    {
                        if (datoSuc.Contains("idSucursal"))
                        {
                            idSucursal = datoSuc.Replace(dato.Substring(0, 8), "").Replace("\"", "").Split(':')[1];
                        }

                        if (datoSuc.Contains("id") && !datoSuc.Contains("idSucursal"))
                        {
                            idTipoFactura = datoSuc.Replace(dato.Substring(0, 8), "").Replace("\"", "").Split(':')[1];
                        }
                    }
                }
            }
        }

        //PASO 11 - RECIBE EL SEGMENTO Y EL CUERPO DEL TXT
        public static bool Cartaporte(string leg, string strtext)
        {
            jsonFactura = "{\r\n\r\n  \"idTipoCfd\":" + "\"" + idTipoFactura + "\"";
            jsonFactura += ",\r\n\r\n  \"nombre\":" + "\"" + leg + ".txt" + "\"";
            jsonFactura += ",\r\n\r\n  \"idSucursal\":" + "\"" + idSucursal + "\"";
            //jsonFactura += ", \r\n\r\n  \"archivoFuente\":" + "\"" + Regex.Replace(strtext, @"\r\n?|\n", "") + "\"" + "\r\n\r\n}";
            jsonFactura += ", \r\n\r\n  \"archivoFuente\":" + "\"" + strtext + "\"" + "\r\n\r\n}";

            string folioFactura = "", serieFactura = "", uuidFactura = "", pdf_xml_descargaFactura = "", pdf_descargaFactura = "", xlm_descargaFactura = "", cancelFactura = "", error = "";
            string salida = "";

            try
            {
                //IdApiEmpresa = "bf2e1036-ba47-49a0-8cd9-e04b36d5afd4";
                //PASO 12 - HACE UNA PETICION PUT A TRALIX PARA TIMBRAR LA CARTAPORTE
                var client = new RestClient("https://canal1.xsa.com.mx:9050/" + "bf2e1036-ba47-49a0-8cd9-e04b36d5afd4" + "/cfdis");
                var request = new RestRequest(Method.PUT);

                request.AddHeader("cache-control", "no-cache");

                request.AddHeader("content-length", "834");
                request.AddHeader("accept-encoding", "gzip, deflate");
                request.AddHeader("Host", "canal1.xsa.com.mx:9050");
                request.AddHeader("Postman-Token", "b6b7d8eb-29f2-420f-8d70-7775701ec765,a4b60b83-429b-4188-98d4-7983acc6742e");
                request.AddHeader("Cache-Control", "no-cache");
                request.AddHeader("Accept", "*/*");
                request.AddHeader("User-Agent", "PostmanRuntime/7.13.0");

                request.AddParameter("application/json", jsonFactura, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                string respuesta = response.StatusCode.ToString();
                //PASO 13 - AQUI VALIDA LA RESPUESTA DE TRALIX Y SI ES OK AVANZA Y SUBE AL FTP E INSERTA EL REGISTRO A VISTA_CARTA_PORTE
                if (respuesta == "BadRequest")
                {
                    return false;
                }
                string[] separadaFactura = response.Content.ToString().Split(',');

                List<string> erroes = new List<string>();

                for (int i = 0; i < 7; i++)
                {
                    try
                    {

                        error = separadaFactura[i].Replace("\\n", "").Replace("]}", "").Replace(@"\", "").Replace("\\t", "").Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "");
                        erroes.Add(error);
                    }
                    catch (Exception)
                    {
                        erroes.Add("N/A");
                    }
                }



                foreach (string factura in separadaFactura)
                {
                    if (factura.Contains("errors") || factura.Contains("error"))
                    {

                        salida = "FALLA AL SUBIR";

                        DateTime fecha1 = DateTime.Now;
                        string fechaFinal = fecha1.Year + "-" + fecha1.Month + "-" + fecha1.Day + " " + fecha1.Hour + ":" + fecha1.Minute + ":" + fecha1.Second + "." + fecha1.Millisecond;

                        facLabControler.ErroresgeneradasCP(fechaFinal, leg, erroes[0], erroes[1], erroes[2], erroes[3], erroes[4], erroes[5], erroes[6]);
                        return false;
                    }
                    else
                    {
                        if (factura.Contains("folio"))
                        {
                            folioFactura = factura.Replace(factura.Substring(0, 5), "").Replace("\"", "").Split(':')[1];
                        }

                        if (factura.Contains("serie"))
                        {
                            serieFactura = factura.Replace(factura.Substring(0, 5), "").Replace("\"", "").Split(':')[1];
                        }

                        if (factura.Contains("uuid"))
                        {
                            uuidFactura = factura.Replace(factura.Substring(0, 4), "").Replace("\"", "").Split(':')[1];
                        }

                        if (factura.Contains("pdfAndXmlDownload"))
                        {
                            pdf_xml_descargaFactura = factura.Replace(factura.Substring(0, 17), "").Replace("\"", "").Split(':')[1];
                        }

                        if (factura.Contains("pdfDownload"))
                        {
                            pdf_descargaFactura = "https://canal1.xsa.com.mx:9050" + factura.Replace(factura.Substring(0, 11), "").Replace("\"", "").Split(':')[1];
                        }

                        if (factura.Contains("xmlDownload") && !factura.Contains("pdfAndXmlDownload"))
                        {
                            xlm_descargaFactura = "https://canal1.xsa.com.mx:9050" + factura.Replace(factura.Substring(0, 11), "").Replace("\"", "").Split(':')[1];
                        }

                        if (factura.Contains("cancellCfdi"))
                        {
                            cancelFactura = factura.Replace(factura.Substring(0, 11), "").Replace("\"", "").Split(':')[1];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string error1 = ex.Message;
            }

            string ftp = System.Web.Configuration.WebConfigurationManager.AppSettings["ftp"];
            if (ftp.Equals("Si"))
            {
                string path = System.Web.Configuration.WebConfigurationManager.AppSettings["dir"] + leg + ".txt";
                UploadFile file = new UploadFile();
            }
            if (salida != "FALLA AL SUBIR")
            {
                if (System.Web.Configuration.WebConfigurationManager.AppSettings["activa"].Equals("Si"))
                {
                    //Modifica referencia
                    string imaging = "http://172.16.136.34/cgi-bin/img-docfind.pl?reftype=ORD&refnum=" + leg.Trim();

                    DateTime fecha1 = Convert.ToDateTime(Fecha);
                    string fechaFinal = fecha1.Year + "-" + fecha1.Month + "-" + fecha1.Day + " " + fecha1.Hour + ":" + fecha1.Minute + ":" + fecha1.Second + "." + fecha1.Millisecond;

                    facLabControler.generadas(folioFactura, serieFactura, uuidFactura, pdf_xml_descargaFactura, pdf_descargaFactura, xlm_descargaFactura, cancelFactura, leg, fechaFinal, Total, Moneda, RFC, Origen, Destino);
                    result.Add(folioFactura);
                    result.Add(serieFactura);
                    result.Add(uuidFactura);
                    result.Add(pdf_xml_descargaFactura);
                    result.Add(pdf_descargaFactura);
                    result.Add(xlm_descargaFactura);
                    result.Add(cancelFactura);
                    result.Add(leg);
                    result.Add(fechaFinal);
                    return true;
                }
                return true;
            }
            else
            {
                return false;//"Error al conectar al servicio XSA";
            }
        }
        public static void iniciaDatos()
        {
            Fecha = words[4].ToString();
            Subtotal = words[5].ToString();
            Totalimptrasl = words[6].ToString();
            Totalimpreten = words[7].ToString();
            Descuentos = words[8].ToString();
            Total = words[9].ToString();
            FormaPago = words[11].ToString();
            Condipago = words[12].ToString();
            MetodoPago = words[13].ToString();
            Moneda = words[14].ToString();
            RFC = words[22].ToString();
            CodSAT = words[39].ToString();
            IdProducto = words[43].ToString();
            Producto = "Viaje";
            Origen = "";// words[321].ToString();
            Destino = "";// words[322].ToString();

            result.Add(Fecha);
            result.Add(Subtotal);
            result.Add(Totalimptrasl);
            result.Add(Totalimpreten);
            result.Add(Descuentos);
            result.Add(Total);
            result.Add(FormaPago);
            result.Add(Condipago);
            result.Add(MetodoPago);
            result.Add(Moneda);
            result.Add(RFC);
            result.Add(CodSAT);
            result.Add(IdProducto);
            result.Add(Producto);
            result.Add(Origen);
            result.Add(Destino);
        }
        public static Hashtable generaActualizacion()
        {
            Hashtable datosTabla = conceptosFinales();
            Hashtable actualiza = new Hashtable();

            foreach (int item in datosTabla.Keys)
            {
                ArrayList list = (ArrayList)datosTabla[item];
                string tipoConcepto = list[3].ToString();
                double total = double.Parse(list[5].ToString());
                if (actualiza.ContainsKey(tipoConcepto))
                {
                    double val = double.Parse(actualiza[tipoConcepto].ToString());
                    actualiza[tipoConcepto] = val + total;
                }
                else
                {
                    actualiza.Add(tipoConcepto, total);
                }
            }
            return actualiza;
        }


        [WebMethod]
        public static object gettable()
        {
            List<CartaPorterest> lista = new List<CartaPorterest>();

            DataTable data = new DataTable();
            data = sql.ObtieneTabla("SELECT TOP 25 Folio, Serie, UUID, Pdf_xml_descarga, Pdf_descargaFactura, replace(xlm_descargaFactura,'}','') as xml_descargaFactura, replace(cancelFactura,'}','') as cancelFactura, LegNum, Fecha, Total, Moneda, RFC,Origen, Destino FROM VISTA_Carta_Porte ORDER BY FECHA DESC");
            if (data.Rows.Count > 0)
            {
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    lista.Add(new CartaPorterest(data.Rows[i][0].ToString(), data.Rows[i][1].ToString(), data.Rows[i][2].ToString(), "<a href=" + '\u0022' + "https://canal1.xsa.com.mx:9050" + data.Rows[i][3].ToString() + '\u0022' + ">" + "<input type=" + '\u0022' + "submit" + '\u0022' + "value=" + '\u0022' + "ZIP" + '\u0022' + "/>" + "</a>", "<a href=" + '\u0022' + data.Rows[i][4].ToString() + '\u0022' + ">" + "<input type=" + '\u0022' + "submit" + '\u0022' + "value=" + '\u0022' + "PDF" + '\u0022' + "/>" + "</a>", "<a href=" + '\u0022' + data.Rows[i][5].ToString() + '\u0022' + ">" + "<input type=" + '\u0022' + "submit" + '\u0022' + "value=" + '\u0022' + "XML" + '\u0022' + "/>" + "</a>", "<button type=" + '\u0022' + "button" + '\u0022' + " OnClick=" + '\u0022' + "cancelCP('" + data.Rows[i][2].ToString() + "'" + ", '" + data.Rows[i][0].ToString() + "' )" + '\u0022' + ">" + "Cancelar" + "</button>", data.Rows[i][7].ToString(), data.Rows[i][8].ToString(), data.Rows[i][9].ToString(), data.Rows[i][10].ToString(), data.Rows[i][11].ToString(), data.Rows[i][12].ToString(), data.Rows[i][13].ToString()));
                }
            }
            object json = new { data = lista };
            return json;
        }

        public static Hashtable conceptosFinales()
        {
            table = new HtmlTable();
            Hashtable datos = new Hashtable();
            for (int i = 0; i < table.Rows.Count - 1; i++)
            {
                TextBox cant = (TextBox)table.FindControl("" + i + "1");
                TextBox unidad = (TextBox)table.FindControl("" + i + "1");
                TextBox concepto = (TextBox)table.FindControl("" + i + "2");
                DropDownList tmp = (DropDownList)table.FindControl("" + i + "3");
                TextBox valor = (TextBox)table.FindControl("" + i + "4");
                TextBox importe = (TextBox)table.FindControl("" + i + "5");

                double cantidad = Math.Abs(double.Parse(cant.Text));

                //double cantidad = Double.Parse(cant.Text);

                ArrayList list = new ArrayList();
                list.Add(cantidad.ToString());
                list.Add(unidad.Text);
                list.Add(concepto.Text);
                list.Add(tmp.SelectedValue);
                list.Add(valor.Text);
                list.Add(importe.Text);

                if (datos.ContainsKey(tmp.Text))
                {
                    datos[i] = list;
                }
                else
                {
                    datos.Add(i, list);
                }
            }
            return datos;
        }

        public void extraer()
        {

            //string ftp = @"C:\Users\Administrator\Documents\SAYER";
            //DirectoryInfo di = new DirectoryInfo(@"C:\Archivos");
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\Administrator\Documents\SAYER");
            FileInfo[] files = di.GetFiles("*.xml");

            int cantidad = files.Length;
            if (cantidad > 0)
            {
                var ultimo_archivo = (from f in di.GetFiles()
                                      orderby f.LastWriteTime descending
                                      select f).First();



                string datestring = DateTime.Now.ToString("yyyyMMddHHmmss");
                string aname = datestring + "-" + ultimo_archivo.Name;
                string farchivo = ultimo_archivo + datestring;
                //Console.WriteLine("Copia existosa: " + farchivo);


                string sourceFile = @"C:\Users\Administrator\Documents\SAYER\" + ultimo_archivo;

                //string destinationFile = @"C:\Archivos\Uploads\" + datestring + "-" + ultimo_archivo;
                string destinationFile = @"C:\inetpub\wwwroot\SWUpload\Uploads\" + datestring + "-" + ultimo_archivo;
                System.IO.File.Move(sourceFile, destinationFile);
                DirectoryInfo dis = new DirectoryInfo(@"C:\inetpub\wwwroot\SWUpload\Uploads");
                FileInfo[] filess = dis.GetFiles("*.xml");
                var lasts = filess.Last();
                cargarEnSQL(aname);
                Console.WriteLine("Copia existosa: " + lasts);
            }
            else
            {
                Console.WriteLine("No hay más archivos");
            }


        }
        public int cargarEnSQL(string narchivo)
        {
            string usuario = "SAYER";
            int resultado = 0;
            try
            {
                //NOS CONECTAMOS CON LA BASE DE DATOS
                string cadena = @"Data source=172.24.16.112; Initial Catalog=TMWSuite; User ID=sa; Password=tdr9312;Trusted_Connection=false;MultipleActiveResultSets=true";
                using (SqlConnection cn = new SqlConnection(cadena))
                {
                    SqlCommand cmd = new SqlCommand("usp_xml", cn);
                    //cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@narchivo", narchivo);

                    cmd.Parameters.Add("Resultado", SqlDbType.Int).Direction = ParameterDirection.Output;

                    cmd.CommandType = CommandType.StoredProcedure;

                    cn.Open();
                    cmd.ExecuteNonQuery();
                    resultado = Convert.ToInt32(cmd.Parameters["Resultado"].Value);

                }

            }
            catch (Exception ex)
            {

                string mensaje = ex.Message.ToString();
                resultado = 0;
            }

            return resultado;
        }




    }
}
