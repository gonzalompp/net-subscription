using Repository.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using UserForms.Models;
using System.Web.Cors;
using web_forms;
using System.Diagnostics.Contracts;

namespace UserForms.Controllers
{
    //var url = "notification/inittester?IDApp=asdasd&IDPlan=2342342&IDCountry=CL&UrlOk=google.cl&UrlError=google.cl&UrlNotify=google.cl&CommerceID=67565454";


    public class RetornoInit
    {
        public string errNumber { get; set; }
        public string errMessage { get; set; }
        public string trx { get; set; }
        public string urlFrmPago { get; set; }
    }

    public class PlanesController : Controller
    {

        ProductsFormsRepository productsFormsRepository = new ProductsFormsRepository();
        ProfilesRepository profilesRepository = new ProfilesRepository();
        CobrosRepository cobrosRepository = new CobrosRepository();
        UsersRepository usersRepository = new UsersRepository();
        ConfigurationsRepository configurationRepository = new ConfigurationsRepository();
        SubscriptionsRepository subscriptionsRepository = new SubscriptionsRepository();
        ProductsRepository productsRepository = new ProductsRepository();

        // GET: PlanesSelector
        [HttpGet]
        public ActionResult Index(string form, string user, string country)
        {
            //validaciones iniciales
            if (String.IsNullOrWhiteSpace(form))
                throw new HttpException(404, "Debes indicar el parametro FORM");

            if (String.IsNullOrWhiteSpace(user))
                throw new HttpException(404, "Debes indicar el parametro USER");



            ViewBag.Pais = country;
            ViewBag.Formulario = form;

            //obtengo el formulario
            var formulario = productsFormsRepository.GetProductForm(form);

            if (formulario == null)
                throw new HttpException(404, "Formulario no existe");

            if (formulario.IdProduct == null)
                throw new HttpException(404, "Formulario no relacionado a un producto");

            if (!usersRepository.UserExist(user, (int)formulario.IdProduct))
            {
                // throw new HttpException(404, "El usuario no existe para este producto");
                //usersRepository.CreateUser(user, (int)formulario.IdProduct);
            }
               

            //obtengo listado de perfiles
            var perfiles = profilesRepository.GetProfiles((int)formulario.IdProduct).Where(e=>e.Featured == true);

            //obtengo perfiles del servicio de COBRO
            var cobros = cobrosRepository.GetCobros((int)formulario.IdProduct); //IDAPP es IDPROD

            List<Planes> planes = new List<Planes>();

            foreach (var item in perfiles)
            {
                var cobro = cobros.FirstOrDefault(e=>e.IdPlan == item.IdProfile && e.Principal == 1);

                planes.Add(new Planes
                {
                    Nombre = item.Name,
                    Valor = (cobro!=null)? cobro.Monto : "-",
                    Plan = item.IdProfile,
                    Caracteristicas = item.Description.Split(','),
                    Featured = item.Featured == null ? false : (bool)item.Featured,
                    Motivator = item.MotivatorText,
                    ShortDescription = item.ShortDescription,
                });
            }

            ViewBag.User = user;

            //obtengo usuario
            var u = usersRepository.GetUser(user, (int)formulario.IdProduct);

            //obtengo suscripcion actual del usuario
            var subs = subscriptionsRepository.GetUserCurrentSubscription(u.IdUser);

            int IdProfile = 0;

            if (subs == null)
            {
                //si no tiene suscripcion, le crea una en el estándar de suscrito sin suscripción.
                var profile = profilesRepository.GetStandardFreeSuscription(u.IdUser);
                IdProfile = profile.IdProfile;
            }
            else
            {
                IdProfile = (int)subs.IdProfile;

                if (subs.PromoActive == true)
                    ViewBag.ActivePromo = 1;
            }

            //datos del formulario
            ViewBag.UrlLogo = formulario.UrlLogo;
            ViewBag.UrlBackground = formulario.UrlBackground;
            ViewBag.ColorSuperiorBar = formulario.ColorSuperiorBar;
            ViewBag.CssClassFeatured = formulario.CssClassFeatured;
            ViewBag.CssClassActual = formulario.CssClassActual;
            ViewBag.TitleColor = formulario.TitleColor;
            ViewBag.FormTitle = formulario.Title;
            ViewBag.CurrentIdProfile = IdProfile;

            return View(planes);
        }

        [AllowCrossSiteAttribute]
        [HttpGet]
        public ActionResult PagarPerfilPremium(string productToken, string pais, string user)
        {
            string baseUrlBillingService = configurationRepository.GetConfig("BaseUrlBillingService");
            string methodUrlBillingService = configurationRepository.GetConfig("MethodUrlBillingService");

            //var url = "notification/inittester?IDApp=asdasd&IDPlan=2342342&IDCountry=CL&UrlOk=google.cl&UrlError=google.cl&UrlNotify=google.cl&CommerceID=67565454";

            //obtengo el producto
            var product_id = productsRepository.GetProductId(productToken);

            var product = productsRepository.GetProduct(product_id);

            var perfilPremium = profilesRepository.GetPaidProfiles(product_id).FirstOrDefault();

            //URLS
            string FreemiumUrlOk = product.BillingUrlOk;
            string FreemiumUrlError = product.BillingUrlError;
            string FreemiumUrlNotify = configurationRepository.GetConfig("FreemiumUrlNotify"); //notifica a freemium sobre la suscripcion
            
            
            string FreemiumCommerceId = configurationRepository.GetConfig("FreemiumCommerceId");

            if (product_id == 1) //MG
            {
                FreemiumCommerceId = configurationRepository.GetConfig("FreemiumCommerceIdMG");
            }
            else if (product_id == 2) //SSZ
            {
                FreemiumCommerceId = configurationRepository.GetConfig("FreemiumCommerceIdSSZ");
            }
            //var url = methodUrlBillingService+"?IDApp=" + formulario.Products.Token+ "&IDPlan="+plan+"&IDCountry="+ pais + "&UrlOk="+ FreemiumUrlOk + "&UrlError="+ FreemiumUrlError + "&UrlNotify="+ FreemiumUrlNotify + "&CommerceID="+ FreemiumCommerceId + "";


            if (product.DemoMode == true)
            {
                //en caso de que el propduicto esté en modo demo, abre la pantalla de pago de prueba
                //return RedirectToAction("Index", "FormularioPago", new { form = form, plan = plan, user = user });
            }

            //llamo init de billing
            Wach.WachHelper billingService = new Wach.WachHelper(baseUrlBillingService);


            var content = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("IDApp", product.Token),
                    new KeyValuePair<string, string>("CommerceID", FreemiumCommerceId),
                    new KeyValuePair<string, string>("IDCountry", pais),
                    new KeyValuePair<string, string>("IDPlan", perfilPremium.IdProfile.ToString()),
                    new KeyValuePair<string, string>("UrlError", FreemiumUrlError),
                    new KeyValuePair<string, string>("UrlNotify", FreemiumUrlNotify),
                    new KeyValuePair<string, string>("UrlOk", FreemiumUrlOk),
                    new KeyValuePair<string, string>("IDUserExternal", user),
                    new KeyValuePair<string, string>("UrlImg", product.BillingUrlProduct), //imagen del logo
                    new KeyValuePair<string, string>("CodigoAnalytics", product.CodeAnalytics) //imagen del logo
                });

            //new EventLogRepository().SetLog("Seleccion planes", "Realizando POST a " + methodUrlBillingService+ " / COD ANALYTICS: "+formulario.Products.CodeAnalytics);

            var respuesta = billingService.PostSimple<RetornoInit>(methodUrlBillingService, content);

            //new EventLogRepository().SetLog("Selección planes", "Respuesta: [" + respuesta.errNumber + "]:" + respuesta.errMessage);

            if (respuesta.errNumber == "0")
            {
                return Redirect(respuesta.urlFrmPago.Replace("{TRX}", respuesta.trx).Replace("{COMM}", FreemiumCommerceId));
            }


            return Content("Error, no se pudo redireccionar");
        }

        [HttpGet]
        public ActionResult Seleccionar(string form, string pais, int plan, string user)
        {
            string baseUrlBillingService = configurationRepository.GetConfig("BaseUrlBillingService");
            string methodUrlBillingService = configurationRepository.GetConfig("MethodUrlBillingService");

            

            //var url = "notification/inittester?IDApp=asdasd&IDPlan=2342342&IDCountry=CL&UrlOk=google.cl&UrlError=google.cl&UrlNotify=google.cl&CommerceID=67565454";


            //obtengo el formulario
            var formulario = productsFormsRepository.GetProductForm(form);

            //URLS
            string FreemiumUrlOk = formulario.Products.BillingUrlOk;
            string FreemiumUrlError = formulario.Products.BillingUrlError;
            string FreemiumUrlNotify = configurationRepository.GetConfig("FreemiumUrlNotify"); //notifica a freemium sobre la suscripcion
            string FreemiumCommerceId = configurationRepository.GetConfig("FreemiumCommerceId");


            //var url = methodUrlBillingService+"?IDApp=" + formulario.Products.Token+ "&IDPlan="+plan+"&IDCountry="+ pais + "&UrlOk="+ FreemiumUrlOk + "&UrlError="+ FreemiumUrlError + "&UrlNotify="+ FreemiumUrlNotify + "&CommerceID="+ FreemiumCommerceId + "";


            if (formulario.Products.DemoMode == true)
            {
                //en caso de que el propduicto esté en modo demo, abre la pantalla de pago de prueba
                return RedirectToAction("Index", "FormularioPago", new { form = form, plan = plan, user = user });
            }

            //llamo init de billing
            Wach.WachHelper billingService = new Wach.WachHelper(baseUrlBillingService);


            var content = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("IDApp", formulario.Products.Token),
                    new KeyValuePair<string, string>("CommerceID", FreemiumCommerceId),
                    new KeyValuePair<string, string>("IDCountry", pais),
                    new KeyValuePair<string, string>("IDPlan", plan.ToString()),
                    new KeyValuePair<string, string>("UrlError", FreemiumUrlError),
                    new KeyValuePair<string, string>("UrlNotify", FreemiumUrlNotify),
                    new KeyValuePair<string, string>("UrlOk", FreemiumUrlOk),
                    new KeyValuePair<string, string>("IDUserExternal", user),
                    new KeyValuePair<string, string>("UrlImg", formulario.Products.BillingUrlProduct), //imagen del logo
                    new KeyValuePair<string, string>("CodigoAnalytics", formulario.Products.CodeAnalytics) //imagen del logo
                });

            //new EventLogRepository().SetLog("Seleccion planes", "Realizando POST a " + methodUrlBillingService+ " / COD ANALYTICS: "+formulario.Products.CodeAnalytics);

            var respuesta = billingService.PostSimple<RetornoInit>(methodUrlBillingService, content);

            //new EventLogRepository().SetLog("Selección planes", "Respuesta: [" + respuesta.errNumber + "]:" + respuesta.errMessage);

            if (respuesta.errNumber == "0")
            {
                return Redirect(respuesta.urlFrmPago.Replace("{TRX}",respuesta.trx).Replace("{COMM}",FreemiumCommerceId));
            }


            return Content("Error, no se pudo redireccionar");
        }



    }
}
