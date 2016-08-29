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
    }
}
