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
            
            //genera objeto de salida
            return new Subscriptions
            {
                //IdSubscription  = subscription.IdSubscription, //no debe ir dentro de lo que se expone como servicio. es de uso interno
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

                IdPromo = subscription.IdPromo
            };

        }

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
                    RenewalDay = DateTime.Now.Day
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
    }
}
