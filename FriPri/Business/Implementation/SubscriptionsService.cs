using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract.Interfaces;
using Contract.Models;
using Repository.Implementation;
using System.Net;
using System.Net.Http;

namespace Business.Implementation
{
    public class SubscriptionsService : ISubscriptionsService
    {

        private ProductsRepository productsRepository { get; set; }
        private UsersRepository usersRepository { get; set; }
        private SubscriptionsRepository subscriptionsRepository { get; set; }
        private ProfilesRepository profilesRepository { get; set; }
        private PromosRepository promosRepository { get; set; }
        private DimensionsRepository dimensionsRepository { get; set; }
        private UsersDimensionsRepository usersDimensionsRepository { get; set; }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public SubscriptionsService()
        {
            this.productsRepository = new ProductsRepository();
            this.usersRepository = new UsersRepository();
            this.subscriptionsRepository = new SubscriptionsRepository();
            this.profilesRepository = new ProfilesRepository();
            this.promosRepository = new PromosRepository();
            this.dimensionsRepository = new DimensionsRepository();
            this.usersDimensionsRepository = new UsersDimensionsRepository();
        }

        public Subscriptions Get(string ProductToken, string UserCode)
        {
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(ProductToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken "+ProductToken, null);

            //obtiene datos del usuario
            var user = usersRepository.GetUser(UserCode,idProduct);
            if (user == null)
            {
                throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);
            }

            if (user.Active == false)
            {
                throw new Contract.Exceptions.UserInactiveException("El usuario indicado no se encuentra Activo en la plataforma",null);
            }

            //obtiene datos del perfil del usuario respecto al producto
            var subscription = subscriptionsRepository.GetUserCurrentSubscription(user.IdUser);

            //perfil segun suscripcion
            Repository.EntityFramework.Profiles profile;

            if (subscription == null)
            {
                //si no tiene suscripcion, le crea una en el estándar de suscrito sin suscripción.
                profile = profilesRepository.GetStandardFreeSuscription(user.IdUser);

                if (profile == null)
                    throw new Contract.Exceptions.StandardProfileNotFoundException("El producto no tiene un perfil estándar para cuando el usuario está registrado pero no está suscrito a ningún perfil. Usuario: " + UserCode, null);

                //setea suscripción vacía 
                subscription = new Repository.EntityFramework.Subscriptions();
            }
            else
            {
                profile = subscription.Profiles;

                if (profile == null) {
                    //si no tiene suscripcion, le crea una en el estándar de suscrito sin suscripción.
                    profile = profilesRepository.GetStandardFreeSuscription(user.IdUser);

                    if (profile == null)
                        throw new Contract.Exceptions.StandardProfileNotFoundException("El producto no tiene un perfil estándar para cuando el usuario está registrado pero no está suscrito a ningún perfil. Usuario: " + UserCode , null);
                }
            }

            List<UsersDimensions> dimensiones = new List<UsersDimensions>();

            foreach (var item in subscription.UsersDimensions)
            {
                dimensiones.Add(new UsersDimensions() {
                    IdDimension = (int)item.IdDimension,
                    Dimension = item.Dimensions.Description,
                    CurrentValue = (decimal)item.CurrentValue,
                    TagName = item.Dimensions.TagName
                });
            }
            
            //genera objeto de salida
            return new Subscriptions
            {
                IdSubscription  = subscription.IdSubscription,
                ExternalCode = subscription.ExternalCode,
                DateCreated = subscription.DateCreated,
                RenewalDay = subscription.RenewalDay,
                LastRenewal = subscription.LastRenewal,
                //IdUser = subscription.IdUser, //no debe ir dentro de lo que se expone como servicio. es de uso interno. Se utiliza el user de abajo como objeto
                IsCurrent = subscription.IsCurrent,

                Users = new Users
                {
                    Active = user.Active,
                    Email = user.Email,
                    ExternalCode = user.ExternalCode
                },

                Profiles = new Profiles
                {
                    ProfileId = profile.IdProfile,
                    Active = profile.Active,
                    Description = profile.Description,
                    Name = profile.Name,
                    TagName = profile.TagName,
                    Paid = profile.Paid
                },

                PromoFreeDays = subscription.PromoFreeDays,

                PromoStarted = subscription.PromoStarted,

                PromoEnd = subscription.PromoEnd,

                PromoActive = subscription.PromoActive,

                IdPromo = subscription.IdPromo,

                Dimensions = dimensiones
            };

        }

        //this is used for billing controller
        public bool SetSubscription(string UserCode, int IdPlan, string AppToken)
        {
            //verificar existencia de la app token
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(AppToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + AppToken, null);

            //obtener usuario segun el app token y external code
            var user = usersRepository.GetUser(UserCode, idProduct);
            if (user == null)
                throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);

            //verificar si existe una suscripcion actual. Si existe se cambia el IDPERFIL. 
            var sub = subscriptionsRepository.GetUserCurrentSubscription(user.IdUser);

            if (sub == null)
            {
                //Si no existe, se crea la suscripcion.

                subscriptionsRepository.NewSubscription(new Repository.EntityFramework.Subscriptions
                {
                    Active = true,
                    DateCreated = DateTime.Now,
                    ExternalCode = "",
                    IdProfile = IdPlan,
                    IdUser = user.IdUser,
                    IsCurrent = true,
                    RenewalDay = DateTime.Now.Day,
                    LastRenewal = DateTime.Now
                });
            }
            else {
                subscriptionsRepository.SetSubscriptionProfile(sub.IdSubscription, IdPlan);
            }

            (new Repository.Implementation.EventLogRepository()).SetLog("POR PINCHAR ANALYTICS","GA");

            try
            {
                //avisa a google analytics si es que tiene el codigo
                var producto = productsRepository.GetProduct(idProduct);

                if (producto != null)
                {
                    (new Repository.Implementation.EventLogRepository()).SetLog("EXISTE PRODUCTO "+producto.IdProduct, "GA");
                    (new Repository.Implementation.EventLogRepository()).SetLog("CODIGO ANALYTICS " + producto.CodeAnalytics, "GA");
                    //si existe el codigo para el producto
                    if (!String.IsNullOrEmpty(producto.CodeAnalytics))
                    {
                        (new Repository.Implementation.EventLogRepository()).SetLog("TIENE CODIGO ANALYTICS " + producto.CodeAnalytics, "GA");
                        var profile = profilesRepository.GetProfile(IdPlan);
                        if (profile != null)
                        {
                            (new Repository.Implementation.EventLogRepository()).SetLog("TIENE PROFILE " + IdPlan + " (" + profile.Name + ")", "GA");
                            GoogleAnalyticsHelper.TrackEvent(producto.CodeAnalytics, @"usuario", @"registro", @"perfil", @"1");
                        }
                    }
                }
            }
            catch (Exception ex)
            { 
                //nothing
            }

            return true;
        }

        public bool SetSubscription(string UserCode, int IdPlan, string AppToken, int addDays)
        {
            //verificar existencia de la app token
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(AppToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + AppToken, null);

            //obtener usuario segun el app token y external code
            var user = usersRepository.GetUser(UserCode, idProduct);
            if (user == null)
                throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);

            //verificar si existe una suscripcion actual. Si existe se cambia el IDPERFIL. 
            var sub = subscriptionsRepository.GetUserCurrentSubscription(user.IdUser);

            if (sub == null)
            {
                //Si no existe, se crea la suscripcion.

                subscriptionsRepository.NewSubscription(new Repository.EntityFramework.Subscriptions
                {
                    Active = true,
                    DateCreated = DateTime.Now,
                    ExternalCode = "",
                    IdProfile = IdPlan,
                    IdUser = user.IdUser,
                    IsCurrent = true,
                    RenewalDay = DateTime.Now.Day,
                    LastRenewal = DateTime.Now.AddDays(addDays)
                });
            }
            else
            {
                //si existe, se setea el nuevo perfily se asigna el tiempo
                subscriptionsRepository.SetSubscriptionProfileWithDays(sub.IdSubscription, IdPlan, addDays);
            }

            return true;
        }

        public bool SetSubscriptionWithPromo(string UserCode, int IdPlan, string AppToken, string TagPromo)
        {
            //verificar existencia de la app token
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(AppToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + AppToken, null);

            //obtener usuario segun el app token y external code
            var user = usersRepository.GetUser(UserCode, idProduct);
            if (user == null)
                throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);

            //voy a buscar la promo
            var promo = this.promosRepository.GetPromo(idProduct, TagPromo);

            //si el tag es nulo o vacio, elimino
            if (String.IsNullOrEmpty(TagPromo))
                promo = null;

            //por si esta mal configurada la promo
            if (promo != null && promo.FreeDays == null)
                promo.FreeDays = 0;

            //voy a buscar el profile por defecto
            var profile = this.profilesRepository.GetStandardFreeSuscription(user.IdUser);
            if (profile != null)
            {
                IdPlan = profile.IdProfile;
            }

            /*
            if (promo == null) {
                //throw new Contract.Exceptions.PromoNotFoundException("No se encontró promocion con Tag " + TagPromo, null);
                //genero promo en blanco para que peuda leer más adelante
                

            }*/

            //verificar si existe una suscripcion actual. Si existe se cambia el IDPERFIL. 
            var sub = subscriptionsRepository.GetUserCurrentSubscription(user.IdUser);

            if (sub == null)
            {
                //Si no existe, se crea la suscripcion.
                Repository.EntityFramework.Subscriptions subs = new Repository.EntityFramework.Subscriptions
                {
                    Active = true,
                    DateCreated = DateTime.Now,
                    ExternalCode = "",
                    IdProfile = IdPlan,
                    IdUser = user.IdUser,
                    IsCurrent = true,
                    RenewalDay = DateTime.Now.Day
                    //LastRenewal = DateTime.Now.AddDays(addDays)
                };

                //si hay promo, le seteo los datos.
                if (promo != null)
                {
                    subs.IdProfile = promo.IdProfileActivePromo; //le setea el perfil que debe ir con la promo, independiente del que se envia por el servicio
                    subs.PromoActive = true;
                    subs.PromoStarted = DateTime.Now;
                    subs.PromoEnd = DateTime.Now.AddDays((int)promo.FreeDays);
                    subs.PromoFreeDays = promo.FreeDays;
                    subs.IdPromo = promo.IdPromo;
                }

                //guardo la nueva subscripcion
                subscriptionsRepository.NewSubscription(subs);
            }
            else
            {
                //si existe, se setea el nuevo perfil y se asigna la promo
                subscriptionsRepository.SetSubscriptionProfileWithPromo(sub.IdSubscription, IdPlan, promo);
            }

            return true;
        }

        public List<ActiveSubscriptionsData> GetActiveSubscriptions(string ProductToken)
        {
            return subscriptionsRepository.GetActiveSubscriptions(ProductToken);
        }

        public bool ResetSubscription(string AppToken, string UserCode)
        {
            //llama con categoria vacia
            return ResetSubscriptionByCategory(AppToken, UserCode, null);
        }

        public bool ResetSubscriptionByCategory(string AppToken, string UserCode, string Category)
        {
            //verificar existencia de la app token
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(AppToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + AppToken, null);

            //obtener usuario segun el app token y external code
            var user = usersRepository.GetUser(UserCode, idProduct);
            if (user == null)
                throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);


            //obtiene la suscripcion
            var sub = subscriptionsRepository.GetUserCurrentSubscription(user.IdUser);

            if (sub == null)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("El usuario "+ UserCode + " del producto "+ AppToken + " no tiene suscripcion", "Sin suscripcion");
                return false;
            }

            //verifica si tiene perfil
            if (sub.IdProfile == null || sub.IdProfile == 0)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("El usuario " + UserCode + " del producto " + AppToken + " no tiene tiene configurado un perfil a su suscripcion", "Sin suscripcion");
                return false;
            }

            /////////////
            // todo ok //
            /////////////

            //obtengo el perfil
            var profile = profilesRepository.GetProfile((int)sub.IdProfile);

            if (profile == null)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("PERFIL ["+ (int)sub.IdProfile + "] NO EXISTE", "Perfil no existente");
                return true;
            }

            //hasta este punto el perfil existe. Se obtienen las dimensiones del perfil
            var dimensiones = dimensionsRepository.GetProfileDimensionsByCategory(profile.IdProfile, Category);

            foreach (var item in dimensiones)
            {
                //chequea si es de tipo consumible
                if (item.IdDimensionType == 3)
                {
                    //si es consumible, verifica que tenga su configuracion para el usuario, si no, la crea
                    var userdimension = item.UsersDimensions.FirstOrDefault(e=>e.IdDimension == item.IdDimension && e.IdSubscription == sub.IdSubscription);

                    if (userdimension == null)
                    {
                        //crea user dimension
                        usersDimensionsRepository.NewUserDimension(item.IdDimension, sub.IdSubscription);
                    }
                    else
                    {
                        usersDimensionsRepository.RestoreUserDimension(userdimension.IdUserDimension);
                    }
                }
            }

            return true;
        }

        public List<SubscriptionsResponse> GetSubscriptionsByRenewal(DateTime CurrentDate, int top)
        {
            var Subscriptions = subscriptionsRepository.GetSubscriptionsByRenewal(CurrentDate, top);

            List<SubscriptionsResponse> response = new List<SubscriptionsResponse>();

            foreach (var item in Subscriptions)
            {
                response.Add(new SubscriptionsResponse {
                    IdSubscription = item.IdSubscription,
                    ExternalCode = item.ExternalCode,
                    DateCreated = item.DateCreated,
                    RenewalDay = item.RenewalDay,
                    LastRenewal = item.LastRenewal,
                    IdUser = item.IdUser,
                    Active = item.Active,
                    IdProfile = item.IdProfile,
                    IsCurrent = item.IsCurrent,
                    PromoFreeDays = item.PromoFreeDays,
                    PromoStarted = item.PromoStarted,
                    PromoEnd = item.PromoEnd,
                    PromoActive = item.PromoActive,
                    IdPromo = item.IdPromo
                });
            }

            return response;
        }
    }
}
