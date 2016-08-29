using Repository.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Implementation
{
    public class ProfilesDimensionsRepository
    {
        private EntityFramework.FriPriEntities db = new EntityFramework.FriPriEntities();

        /// <summary>
        /// Obtiene miension del perfil actual para el usuario indicado
        /// </summary>
        /// <param name="idProduct"></param>
        /// <param name="idUser"></param>
        /// <param name="DimensionTag"></param>
        /// <returns></returns>
        public ProfilesDimensions GetProfileDimension(int idUser, int idDimension)
        {
            return db.ProfilesDimensions.FirstOrDefault(e => e.Profiles.Subscriptions.Any(j => j.Users.IdUser == idUser && j.IsCurrent == true) && e.IdDimension == idDimension);
        }
    }
}
