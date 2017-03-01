using Contract.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Implementation
{
    public class SubscriptionsRepository
    {
        public EntityFramework.FriPriEntities db = new EntityFramework.FriPriEntities();

        public EntityFramework.Subscriptions GetUserCurrentSubscription(int idUser)
        {
            //primero obtengo la suscripcion del usuario por defecto
            var subscription = db.Subscriptions.FirstOrDefault(
                e=>
                e.IdUser == idUser && 
                e.IsCurrent == true && 
                e.Active == true
            );

            return subscription;
        }

        public EntityFramework.Subscriptions NewSubscription(EntityFramework.Subscriptions subs)
        {
            try
            {
                db.Subscriptions.Add(subs);
                db.SaveChanges();

                return subs;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public EntityFramework.Subscriptions GetSubscription(int idSubscription)
        {
                return db.Subscriptions.FirstOrDefault(e => e.IdSubscription == idSubscription);
        }

        public EntityFramework.Subscriptions SetSubscriptionProfile(int idSubscription, int idProfile)
        {
            var sus = db.Subscriptions.FirstOrDefault(e => e.IdSubscription == idSubscription);

            if (sus == null)
                return null;

            sus.IdProfile = idProfile;
            sus.IdPromo = null;
            sus.PromoActive = null;
            sus.PromoEnd = null;
            sus.PromoFreeDays = null;
            sus.PromoStarted = null;

            db.SaveChanges();

            return sus;
        }

        public EntityFramework.Subscriptions SetSubscriptionProfileWithDays(int idSubscription, int idProfile, int days)
        {
            

            var sus = db.Subscriptions.FirstOrDefault(e => e.IdSubscription == idSubscription);

            if (sus == null)
                return null;

            sus.IdProfile = idProfile;

            if (sus.LastRenewal == null)
            {
                new EventLogRepository().SetLog("Agregando dias", "LastRenewal es nulo, se coloca la fecha de hoy ("+DateTime.Now+")");
                sus.LastRenewal = DateTime.Now;
            }

            
            new EventLogRepository().SetLog("Agregando dias", "LastRenewal tiene valor " + sus.LastRenewal.ToString());
            new EventLogRepository().SetLog("Agregando dias", "Se agregarán " + days + " dias");

            //agrega dias
            sus.LastRenewal = sus.LastRenewal.Value.AddDays(days);

            new EventLogRepository().SetLog("Agregando dias", "LastRenewal tiene valor " + sus.LastRenewal.ToString());

            db.SaveChanges();

            return sus;
        }

        public EntityFramework.Subscriptions SetSubscriptionProfileWithPromo(int idSubscription, int idProfile, EntityFramework.Promos promo)
        {
            new EventLogRepository().SetLog("SetSubscriptionProfileWithPromo", "Ingresa a metodo IDSUBS[" + idSubscription + "] IDPROF[" + idProfile + "]");

            var sus = db.Subscriptions.FirstOrDefault(e => e.IdSubscription == idSubscription);

            if (sus == null)
                return null;

            sus.IdProfile = idProfile;

            if (sus.LastRenewal == null)
            {
                new EventLogRepository().SetLog("Agregando dias", "LastRenewal es nulo, se coloca la fecha de hoy (" + DateTime.Now + ")");
                sus.LastRenewal = DateTime.Now;
            }


            new EventLogRepository().SetLog("Agregando dias", "LastRenewal tiene valor " + sus.LastRenewal.ToString());
            //new EventLogRepository().SetLog("Agregando dias", "Se agregarán " + days + " dias");

            //agrega dias
            //sus.LastRenewal = sus.LastRenewal.Value.AddDays(days);
            
            //se asigna la promo
            if (promo != null)
            {
                sus.IdProfile = promo.IdProfileActivePromo; //le setea el perfil que debe ir con la promo, independiente del que se envia por el servicio
                sus.PromoActive = true;
                sus.PromoStarted = DateTime.Now;
                sus.PromoEnd = DateTime.Now.AddDays((int)promo.FreeDays);
                sus.PromoFreeDays = promo.FreeDays;
                sus.IdPromo = promo.IdPromo;
            }

            new EventLogRepository().SetLog("Agregando dias", "LastRenewal tiene valor " + sus.LastRenewal.ToString());

            db.SaveChanges();

            return sus;
        }

        public List<EntityFramework.Subscriptions> GetSubscriptionsByRenewal(DateTime CurrentDate, int top)
        {
            //DateTime Today = DateTime.Now;
            int DaysInMonth = DateTime.DaysInMonth(CurrentDate.Year,CurrentDate.Month);

            //el primer dia del mes de la fecha actual
            DateTime fecha_limite_lastrenewal = CurrentDate.AddDays((CurrentDate.Day - 1) * (-1));

            //obtiene las subscripciones que no hayan sido renovadas este mes (Busca renovaciones de meses anteriores)
            var Subscriptions = db.Subscriptions.Where(e=> e.LastRenewal < fecha_limite_lastrenewal);

            //si el mes termina en este dia, que obtenga las suscripciones que sean de dia mayor (Todas las restantes del mes)
            if (CurrentDate.Day == DaysInMonth)
                Subscriptions = Subscriptions.Where(e => e.RenewalDay >= CurrentDate.Day);
            else
                Subscriptions = Subscriptions.Where(e => e.RenewalDay == CurrentDate.Day);

            //cantidad maxima
            if (top > 0)
                Subscriptions = Subscriptions.Take(top);

            return Subscriptions.ToList();

        }

        public List<ActiveSubscriptionsData> GetActiveSubscriptions(string ProductToken)
        {
            List<ActiveSubscriptionsData> list = new List<ActiveSubscriptionsData>();

            var subs = db.Subscriptions.Where(e=>e.Users.Products.Token == ProductToken && !e.Unsubscriptions.Any()).OrderBy(e=>e.DateCreated);

            foreach (var item in subs)
            {
                list.Add(new ActiveSubscriptionsData
                {
                    ProfileId = (int)item.IdProfile,
                    ProfileName = item.Profiles.Name,
                    ProfilePaid = (item.Profiles != null && item.Profiles.Paid != null) ? (bool)item.Profiles.Paid : false,
                    SubscriptionDateCreated = item.DateCreated,
                    UserExternalCode = item.Users.ExternalCode.Trim(),
                    SubscriptionDateLastRenewal = item.LastRenewal
                });
            }

            return list;
        }


    }
}
