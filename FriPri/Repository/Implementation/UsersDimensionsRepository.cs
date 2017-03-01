﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Implementation
{
    public class UsersDimensionsRepository
    {
        private EntityFramework.FriPriEntities db = new EntityFramework.FriPriEntities();

        public EntityFramework.UsersDimensions NewUserDimension(int IdDimension, int IdSubscription)
        {
            if (IdDimension == 0 || IdSubscription == 0)
                return null;

            var userdimension = new EntityFramework.UsersDimensions();

            userdimension.IdDimension = IdDimension;
            userdimension.DateCreated = DateTime.Now;
            userdimension.DateLastUpdate = DateTime.Now;
            userdimension.IdSubscription = IdSubscription;
            userdimension.Active = true;

            //ahora setea el valor por defecto si es que tiene relacion entre dimension y perfil del usuario
            //var dimension = db.Dimensions.FirstOrDefault(e => e.IdDimension == IdDimension);
            var profiledimension = db.ProfilesDimensions.Where(e => e.Profiles.Subscriptions.Any(j => j.IdSubscription == IdSubscription) && e.IdDimension == IdDimension).FirstOrDefault();

            //le inserto su valor por defecto
            userdimension.CurrentValue = profiledimension.Value;

            db.UsersDimensions.Add(userdimension);
            db.SaveChanges();

            return userdimension;
        }

        public EntityFramework.UsersDimensions RestoreUserDimension(int IdUserDimension)
        {
            if (IdUserDimension == 0)
                return null;
            var userdimension = db.UsersDimensions.FirstOrDefault(e => e.IdUserDimension == IdUserDimension);

            //ahora setea el valor por defecto
            var profiledimension = db.ProfilesDimensions.Where(e => e.Profiles.Subscriptions.Any(j => j.IdSubscription == userdimension.IdSubscription) && e.IdDimension == userdimension.IdDimension).FirstOrDefault();

            //le inserto su valor por defecto
            userdimension.CurrentValue = profiledimension.Value;

            db.SaveChanges();

            return userdimension;
        }

        public EntityFramework.UsersDimensions GetUserDimension(int IdDimension, int IdSubscription)
        {
            if (IdDimension == 0 || IdSubscription == 0)
                return null;

            return db.UsersDimensions.FirstOrDefault(e => e.IdSubscription == IdSubscription && e.IdDimension == IdDimension);
        }

        public EntityFramework.UsersDimensions ConsumeAndGetUserDimension(int IdDimension, int IdSubscription, decimal Amount)
        {
            if (IdDimension == 0 || IdSubscription == 0)
                return null;

            //si el monto a descontar es numero negativo, lo paso a positivo
            if (Amount < 0)
                Amount = Amount * (-1);

            var userdimension = db.UsersDimensions.FirstOrDefault(e => e.IdSubscription == IdSubscription && e.IdDimension == IdDimension);

            //descuento el valor
            userdimension.CurrentValue -= Amount;

            //si el descuento queda negativo, se deja en 0
            if (userdimension.CurrentValue < 0)
                userdimension.CurrentValue = 0;

            //actualizo fecha de ultima modificacion
            userdimension.DateLastUpdate = DateTime.Now;

            db.SaveChanges();

            //retorna
            return userdimension;
        }

        public decimal ConsumeAndGetConsumedUserDimensionValue(int IdDimension, int IdSubscription, decimal Amount)
        {
            //if (IdDimension == 0 || IdSubscription == 0)
            //    return null;

            //si el monto a descontar es numero negativo, lo paso a positivo
            if (Amount < 0)
                Amount = Amount * (-1);

            var userdimension = db.UsersDimensions.FirstOrDefault(e => e.IdSubscription == IdSubscription && e.IdDimension == IdDimension);

            decimal original_value = (decimal)userdimension.CurrentValue;

            //descuento el valor
            userdimension.CurrentValue -= Amount;

            //si el descuento queda negativo, se deja en 0
            if (userdimension.CurrentValue < 0)
                userdimension.CurrentValue = 0;

            //actualizo fecha de ultima modificacion
            userdimension.DateLastUpdate = DateTime.Now;

            db.SaveChanges();

            //retorna la diferencia
            return original_value - (decimal)userdimension.CurrentValue;
        }
    }
}
