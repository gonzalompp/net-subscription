using Contract.Models;
using Newtonsoft.Json;
using Repository.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api.Controllers
{
    [RoutePrefix("api/naranya")]
    public class NaranyaController : ApiController
    {
        private NaranyaNotificationRepository naranyaNotificationRepository { get; set; }

        public NaranyaController()
        {
            this.naranyaNotificationRepository = new NaranyaNotificationRepository();
        }

        [Route("notification")]
        public bool Notification([FromBody]NaranyaNotification data)
        {
            (new Repository.Implementation.EventLogRepository()).SetLog("Naranya Subscription Notification", "Ingresa notificacion de Naranya: Event["+ data.id_event+"] / Trans["+data.id_transaction+"]");

            //almacena notificacion
            this.naranyaNotificationRepository.SaveNewNotification(data);

            string url_ack = "https://ipn.npay.io/verify/" + data.GetUrlParamsTres();

            // POST query to NPay website, containing the same data that we received
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_ack);
            request.Method = "POST";
            request.ContentType = "application/json";

            /*

            var body = JsonConvert.SerializeObject(data);

            using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(body);
                streamWriter.Flush();
                streamWriter.Close();
            }

            */

            try
            {

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Console.WriteLine(String.Format("StatusCode == {0}", response.StatusCode));
                    Console.WriteLine(sr.ReadToEnd());

                    (new Repository.Implementation.EventLogRepository()).SetLog("Naranya Subscription Notification", "Notificacion con ACK exitoso: Event[" + data.id_event + "] / Trans[" + data.id_transaction + "]");

                }

            }
            catch (Exception ex)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("Naranya Subscription Notification EXCEPTION", "Mensaje: "+@ex.Message);

            }

            return true;
        }
    }
}
