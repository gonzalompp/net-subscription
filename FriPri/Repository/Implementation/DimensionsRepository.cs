using Repository.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Implementation
{
    public class DimensionsRepository
    {
        private EntityFramework.FriPriEntities db = new EntityFramework.FriPriEntities();

        public Dimensions GetDimension(int idProduct, string DimensionTag)
        {
            return db.Dimensions.FirstOrDefault(e => e.DimensionsCategories.IdProduct == idProduct && e.TagName == DimensionTag);
        }

        public Dimensions GetDimension(int idDimension)
        {
            return db.Dimensions.FirstOrDefault(e => e.IdDimension == idDimension);
        }

        public List<Dimensions> GetProfileDimensions(int IdProfile)
        {
            if (IdProfile == 0)
                return null;

            return db.Dimensions.Where(e=>e.ProfilesDimensions.Any(j=>j.IdProfile == IdProfile)).ToList();
        }

        public List<Dimensions> GetProfileDimensionsByCategory(int IdProfile, string Category)
        {
            if (IdProfile == 0)
                return null;

            var dims = db.Dimensions.Where(e => e.ProfilesDimensions.Any(j => j.IdProfile == IdProfile));

            //filtra por categoria si es que la trae
            if (Category != null)
                dims = dims.Where(e=>e.DimensionsCategories.TagName == Category);

            return dims.ToList();
        }
    }
}
