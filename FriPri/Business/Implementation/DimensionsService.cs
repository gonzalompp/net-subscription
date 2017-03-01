using Contract.Interfaces;
using Repository.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Implementation
{
    public class DimensionsService : IDimensionsService
    {
        private ProductsRepository productsRepository { get; set; }
        private UsersRepository usersRepository { get; set; }
        private ProfilesDimensionsRepository profilesDimensionsRepository { get; set; }
        private DimensionsRepository dimensionsRepository { get; set; }
        private ProfilesRepository profilesRepository { get; set; }
        private UsersDimensionsRepository usersDimensionsRepository { get; set; }
        private SubscriptionsRepository subscriptionsRepository { get; set; }

        public DimensionsService()
        {
            this.productsRepository = new ProductsRepository();
            this.usersRepository = new UsersRepository();
            this.profilesDimensionsRepository = new ProfilesDimensionsRepository();
            this.dimensionsRepository = new DimensionsRepository();
            this.profilesRepository = new ProfilesRepository();
            this.usersDimensionsRepository = new UsersDimensionsRepository();
            this.subscriptionsRepository = new SubscriptionsRepository();
        }

        public decimal GetNumericDimension(string ProductToken, string UserCode, string DimensionTag)
        {

            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(ProductToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + ProductToken, null);

            //validar existencia de dimension buscada
            Repository.EntityFramework.Dimensions dimension = dimensionsRepository.GetDimension(idProduct, DimensionTag);
            if (dimension == null)
            {
                throw new Contract.Exceptions.ProductDimensionNotFoundException("La dimensión " + DimensionTag + "(" + dimension.IdDimension + ") no existe para este producto", null);
            }

            //obtiene datos del usuario
            var user = usersRepository.GetUser(UserCode, idProduct);
            if (user == null)
            {
                //throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);

                //si el usuario no existe, le retorna el valor del perfil anonimo para el producto
                return GetAnonNumericDimension(idProduct, dimension.IdDimension);
            }

            if (user.Active == false)
            {
                throw new Contract.Exceptions.UserInactiveException("El usuario indicado no se encuentra Activo en la plataforma", null);
            }

            //ahora, al ser de tipo switch, ve directamente el valor para el perfil del usuario y lo retorna
            var profiledimension = profilesDimensionsRepository.GetProfileDimension(user.IdUser, dimension.IdDimension);

            if (profiledimension == null)
            {
                //si la dimensión no está registrada en el perfil, arroja el valor por defecto de la dimension

                if (dimension.Value == null)
                {
                    throw new Contract.Exceptions.ProfileDimensionNoDefaultValueException("La dimensión " + DimensionTag + "(" + dimension.IdDimension + ") no tiene un valor por defecto.", null);
                }

                return (decimal)dimension.Value;

                //throw new Contract.Exceptions.ProfileDimensionNotConfiguredException("La dimensión " + DimensionTag + " no está bien configurada en todos los perfiles. No se encontro una relación entre el perfil actual del usuario y la dimensión.", null);
            }

            if (profiledimension.Dimensions.DimensionsTypes == null)
            {
                throw new Contract.Exceptions.DimensionTypeNotConfiguredSwitchException("La dimension " + DimensionTag + " No tiene tipo (Switch/Numeric/NumericConsumable) ", null);
            }

            if (!profiledimension.Dimensions.DimensionsTypes.TagName.Equals("Numeric"))
            {
                throw new Contract.Exceptions.DimensionTypeIsNotSwitchException("La dimension " + DimensionTag + " NO es de tipo Numérica, es de tipo " + profiledimension.Dimensions.DimensionsTypes.TagName, null);
            }

            //retorno
            return (decimal)profiledimension.Value;
        }

        public decimal GetAnonNumericDimension(int IdProduct, int IdDimension)
        {
            //obtiene al producto
            var product = productsRepository.GetProduct(IdProduct);
            if (product == null)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el id " + IdProduct, null);

            //validar existencia de dimension buscada
            Repository.EntityFramework.Dimensions dimension = dimensionsRepository.GetDimension(IdDimension);
            if (dimension == null)
            {
                throw new Contract.Exceptions.ProductDimensionNotFoundException("La dimensión (" + IdDimension + ") no existe para este producto", null);
            }

            //ahora va a buscar el perfil anonimo por defecto del producto
            var profile = profilesRepository.GetProfileByAnon(IdProduct);

            var profiledimension = profile.ProfilesDimensions.FirstOrDefault(e=>e.IdDimension == IdDimension);

            //validar existencia de dimension del perfil anonimo buscada
            if (profiledimension == null)
            {
                throw new Contract.Exceptions.ProfileDimensionAnonNoConfiguredException("El producto (" + IdProduct + ") no tiene configurado ninguna relacion para usuario ANONIMO con la dimension (" + IdDimension + ")", null);
            }

            if (profiledimension.Value == null)
            {
                throw new Contract.Exceptions.ProfileDimensionAnonNoConfiguredException("La profiledimension(" + profiledimension.IdProfileDimension + ") no tiene ningún valor SWITCH por defecto", null);

            }

            //ahora va a buscar la dimension y su valor por defecto para ese perfil
            return (decimal)profiledimension.Value;
        }

        public bool GetAnonSwitchDimension(int IdProduct, int IdDimension)
        {
            //obtiene al producto
            var product = productsRepository.GetProduct(IdProduct);
            if (product == null)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el id " + IdProduct, null);

            //validar existencia de dimension buscada
            Repository.EntityFramework.Dimensions dimension = dimensionsRepository.GetDimension(IdDimension);
            if (dimension == null)
            {
                throw new Contract.Exceptions.ProductDimensionNotFoundException("La dimensión ("+ IdDimension + ") no existe para este producto", null);
            }

            //ahora va a buscar el perfil anonimo por defecto del producto
            var profile = profilesRepository.GetProfileByAnon(IdProduct);

            var profiledimension = profile.ProfilesDimensions.FirstOrDefault(e => e.IdDimension == IdDimension);

            //validar existencia de dimension del perfil anonimo buscada
            if (profiledimension == null)
            {
                throw new Contract.Exceptions.ProfileDimensionAnonNoConfiguredException("El producto ("+IdProduct+") no tiene configurado ninguna relacion para usuario ANONIMO con la dimension ("+IdDimension+")", null);
            }

            if (profiledimension.SwitchValue == null)
            {
                throw new Contract.Exceptions.ProfileDimensionAnonNoConfiguredException("La profiledimension("+ profiledimension.IdProfileDimension + ") no tiene ningún valor SWITCH por defecto", null);

            }

            //ahora va a buscar la dimension y su valor por defecto para ese perfil
            return (bool)profiledimension.SwitchValue;

        }

        public bool GetSwitchDimension(string ProductToken, string UserCode, string DimensionTag)
        {
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(ProductToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + ProductToken, null);

            //validar existencia de dimension buscada
            Repository.EntityFramework.Dimensions dimension = dimensionsRepository.GetDimension(idProduct, DimensionTag);
            if(dimension == null)
            {
                throw new Contract.Exceptions.ProductDimensionNotFoundException("La dimensión "+ DimensionTag + "(" + dimension.IdDimension + ") no existe para este producto", null);
            }

            //obtiene datos del usuario
            var user = usersRepository.GetUser(UserCode,idProduct);
            if (user == null)
            {
                //throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);

                //si el usuario no existe, le retorna el valor del perfil anonimo para el producto
                return GetAnonSwitchDimension(idProduct,dimension.IdDimension);
            }

            if (user.Active == false)
            {
                throw new Contract.Exceptions.UserInactiveException("El usuario indicado no se encuentra Activo en la plataforma", null);
            }

            //ahora, al ser de tipo switch, ve directamente el valor para el perfil del usuario y lo retorna
            var profiledimension = profilesDimensionsRepository.GetProfileDimension(user.IdUser, dimension.IdDimension);

            if (profiledimension == null)
            {
                //si la dimensión no está registrada en el perfil, arroja el valor por defecto de la dimension

                if (dimension.SwitchValue == null)
                {
                    throw new Contract.Exceptions.ProfileDimensionNoDefaultValueException("La dimensión " + DimensionTag + "("+ dimension.IdDimension + ") no tiene un valor por defecto.", null);
                }

                return (bool)dimension.SwitchValue;

                //throw new Contract.Exceptions.ProfileDimensionNotConfiguredException("La dimensión " + DimensionTag + " no está bien configurada en todos los perfiles. No se encontro una relación entre el perfil actual del usuario y la dimensión.", null);
            }

            if (profiledimension.Dimensions.DimensionsTypes == null)
            {
                throw new Contract.Exceptions.DimensionTypeNotConfiguredSwitchException("La dimension " + DimensionTag + " No tiene tipo (Switch/Numeric/NumericConsumable) ", null);
            }

            if (!profiledimension.Dimensions.DimensionsTypes.TagName.Equals("Switch"))
            {
                throw new Contract.Exceptions.DimensionTypeIsNotSwitchException("La dimension " + DimensionTag + " NO es de tipo Switch, es de tipo "+ profiledimension.Dimensions.TagName, null);
            }

            //retorno
            return (bool)profiledimension.SwitchValue;
        }

        /**********************************************************************************/

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public decimal GetNumericConsumableDimension(string ProductToken, string UserCode, string DimensionTag)
        {
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(ProductToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + ProductToken, null);

            //validar existencia de dimension buscada
            Repository.EntityFramework.Dimensions dimension = dimensionsRepository.GetDimension(idProduct, DimensionTag);
            if (dimension == null)
            {
                throw new Contract.Exceptions.ProductDimensionNotFoundException("La dimensión " + DimensionTag + "(" + dimension.IdDimension + ") no existe para este producto", null);
            }

            //obtiene datos del usuario
            var user = usersRepository.GetUser(UserCode, idProduct);
            if (user == null)
            {
                throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);
            }

            if (user.Active == false)
            {
                throw new Contract.Exceptions.UserInactiveException("El usuario indicado no se encuentra Activo en la plataforma", null);
            }

            //obtengo la suscripcion
            //obtiene la suscripcion
            var sub = subscriptionsRepository.GetUserCurrentSubscription(user.IdUser);

            if (sub == null)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("El usuario " + UserCode + " del producto " + ProductToken + " no tiene suscripcion", "Sin suscripcion");
                throw new Contract.Exceptions.SubscriptionNotFoundException("No se encontró una subscripcion para el UserCode " + UserCode, null);
            }

            if (sub.IdProfile == 0)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("El usuario " + UserCode + " del producto " + ProductToken + " no tiene perfil en su suscripcion", "Sin perfil en susc.");
                throw new Contract.Exceptions.ProfileNotFoundException("No se encontró un perfil configurado para la suscripcion del usuario", null);
            }


            //obtengo el userdimension
            var userdimension = usersDimensionsRepository.GetUserDimension(dimension.IdDimension, sub.IdSubscription);

            if (userdimension == null)
                throw new Contract.Exceptions.UserDimensionNotFoundException("Usuario no tiene configurada la dimension", null);

            //TODO OK

            return (decimal)userdimension.CurrentValue;

        }

        decimal IDimensionsService.ConsumeNumericDimension(string ProductToken, string UserCode, string DimensionTag, decimal amount)
        {
            throw new NotImplementedException();
        }

        public decimal ConsumeNumericConsumableDimension(string ProductToken, string UserCode, string DimensionTag, decimal Amount)
        {
            //obtiene ID de producto
            int idProduct = productsRepository.GetProductId(ProductToken);
            if (idProduct == 0)
                throw new Contract.Exceptions.ProductsNotFoundException("No se encontró un producto con el ProductToken " + ProductToken, null);

            //validar existencia de dimension buscada
            Repository.EntityFramework.Dimensions dimension = dimensionsRepository.GetDimension(idProduct, DimensionTag);
            if (dimension == null)
            {
                throw new Contract.Exceptions.ProductDimensionNotFoundException("La dimensión " + DimensionTag + "(" + dimension.IdDimension + ") no existe para este producto", null);
            }

            //obtiene datos del usuario
            var user = usersRepository.GetUser(UserCode, idProduct);
            if (user == null)
            {
                throw new Contract.Exceptions.UserNotFoundException("No se encontró un usuario con el UserCode " + UserCode, null);
            }

            if (user.Active == false)
            {
                throw new Contract.Exceptions.UserInactiveException("El usuario indicado no se encuentra Activo en la plataforma", null);
            }

            //obtengo la suscripcion
            //obtiene la suscripcion
            var sub = subscriptionsRepository.GetUserCurrentSubscription(user.IdUser);

            if (sub == null)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("El usuario " + UserCode + " del producto " + ProductToken + " no tiene suscripcion", "Sin suscripcion");
                throw new Contract.Exceptions.SubscriptionNotFoundException("No se encontró una subscripcion para el UserCode " + UserCode, null);
            }

            if (sub.IdProfile == 0)
            {
                (new Repository.Implementation.EventLogRepository()).SetLog("El usuario " + UserCode + " del producto " + ProductToken + " no tiene perfil en su suscripcion", "Sin perfil en susc.");
                throw new Contract.Exceptions.ProfileNotFoundException("No se encontró un perfil configurado para la suscripcion del usuario", null);
            }

            //obtengo el profile dimension
            var profiledimension = this.profilesDimensionsRepository.GetProfileDimension(user.IdUser, dimension.IdDimension);
            if (profiledimension == null)
            {
                throw new Contract.Exceptions.ProfileDimensionNotConfiguredException("La dimension no fue configurada con el perfil", null);
            }


            if (profiledimension.IsInfinite == true)
            {
                //si la dimension es infinita, solo devuelve el valor que le pasaron como parametro
                //var userdimension = usersDimensionsRepository.GetUserDimension(dimension.IdDimension, sub.IdSubscription);

                //return (decimal)userdimension.CurrentValue;
                return Amount;
            }
            else
            {
                //si la dimension NO ES FININITA consume y devuelve valor
                /*
                var userdimension = usersDimensionsRepository.ConsumeAndGetUserDimension(dimension.IdDimension, sub.IdSubscription, Amount);

                if (userdimension == null)
                    throw new Contract.Exceptions.UserDimensionNotFoundException("Usuario no tiene configurada la dimension", null);
                    */

                return usersDimensionsRepository.ConsumeAndGetConsumedUserDimensionValue(dimension.IdDimension, sub.IdSubscription, Amount);
            }
        }
    }
}
