using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract.Interfaces;
using Contract.Models;

namespace Business.Integration
{
    public class DimensionsService : IDimensionsService
    {

        /*
            Contract.Models.IntegrationValues.NumericValue = NumericValue;
            Contract.Models.IntegrationValues.SwitchValue = SwitchValue;
            Contract.Models.IntegrationValues.UserIsActive = UserIsActive;
        */

        public bool ConsumeNumericDimension(string ProductToken, string UserCode, string DimensionTag, decimal quantity)
        {
            Contract.Models.IntegrationValues.NumericValue = Contract.Models.IntegrationValues.NumericValue - quantity;
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool GetAnonSwitchDimension(int IdProduct, int IdDimension)
        {
            throw new NotImplementedException();
        }

        public decimal GetNumericDimension(string ProductToken, string UserCode, string DimensionTag)
        {
            return Contract.Models.IntegrationValues.NumericValue;
        }

        public bool GetSwitchDimension(string ProductToken, string UserCode, string DimensionTag)
        {
            return Contract.Models.IntegrationValues.SwitchValue;
        }
    }
}
