using Repository.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Implementation
{
    public class ProfilesRepository
    {
        public EntityFramework.FriPriEntities db = new EntityFramework.FriPriEntities();

        public EntityFramework.Profiles GetProfile(int IdProfile)
        {
            return db.Profiles.FirstOrDefault(e => e.IdProfile == IdProfile);
        }

        public EntityFramework.Profiles GetStandardFreeSuscription(int idUser)
        {
            //obtengo al usuario
            var user = db.Users.FirstOrDefault(e => e.IdUser == idUser);

            //obtengo al perfil estándar free del producto al que pertenece el usuario
            return db.Profiles.FirstOrDefault(e => e.IdProduct == user.IdProduct && e.UserDefault == true);
        }

        public List<EntityFramework.Profiles> GetProfiles(int idProduct)
        {
            return db.Profiles.Where(e=>e.IdProduct == idProduct).ToList();
        }

        public List<EntityFramework.Profiles> GetPaidProfiles(int idProduct)
        {
            return db.Profiles.Where(e => e.IdProduct == idProduct && e.Paid == true).ToList();
        }

        public Profiles GetProfileByAnon(int IdProduct)
        {
            //return db.ProfilesDimensions.FirstOrDefault(e=>e.Profiles.IdProduct == IdProduct && e.Profiles.AnonDefault == true);
            return db.Profiles.FirstOrDefault(e => e.IdProduct == IdProduct && e.AnonDefault == true);
        }
    }
}
