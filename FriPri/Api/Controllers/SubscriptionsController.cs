using Business.Integration;
using Contract.Interfaces;
using Contract.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api.Controllers
{
    [RoutePrefix("api/subscriptions")]
    public class SubscriptionsController : ApiController
    {
        private ISubscriptionsService subscriptionsService;

        public SubscriptionsController(ISubscriptionsService _subscriptionsService)
        {
            this.subscriptionsService = _subscriptionsService;
        }


        [Route("for-renewal")]
        [HttpPost]
        public List<SubscriptionsResponse> GetSubscriptionsByRenewal(GetSubscriptionsByRenewalParams data)
        {
            //limpia
            if (data.top < 0)
                data.top = 0;

            return this.subscriptionsService.GetSubscriptionsByRenewal(DateTime.Parse(data.currentDate), data.top);
        }


        //SetSubscription([FromBody]NotifySubscription data)
        [Route("reset")]
        [HttpPut]
        public bool Reset([FromBody]ResetData data)
        {
            try
            {
                return this.subscriptionsService.ResetSubscription(data.ProductToken, data.IdUserExternal);
            }
            catch (Contract.Exceptions.ProductsNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Contract.Exceptions.UserNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Contract.Exceptions.UserInactiveException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error desconocido en el servidor"));
                //throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));

            }
        }

        [Route("active-subscriptions/")]
        [HttpPut]
        public List<ActiveSubscriptionsData> Reset([FromBody]ActiveSubscriptionsParam data)
        {
            
            try
            {
                return this.subscriptionsService.GetActiveSubscriptions(data.ProductToken);
            }
            catch (Contract.Exceptions.ProductsNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Contract.Exceptions.UserNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Contract.Exceptions.UserInactiveException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error desconocido en el servidor: "));
            }
        }

        [Route("renew/")]
        [HttpPut]
        public bool Reset([FromBody]ResetSubscriptionParam data)
        {
            try
            {
                return this.subscriptionsService.ResetSubscriptionByCategory(data.ProductToken, data.UserExternalCode, data.CategoryNameTag);
            }
            catch (Contract.Exceptions.ProductsNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Contract.Exceptions.UserNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Contract.Exceptions.UserInactiveException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error desconocido en el servidor: "));
            }
        }


        /// <summary>
        /// Reset the subscription. If the subscription is related to numeric consumption dimension type, it will reset the values to the original amount.
        /// </summary>
        /// <param name="ProductToken"></param>
        /// <param name="UserCode"></param>
        /// <returns></returns>
        [Route("reset/{ProductToken}/{UserCode}")]
        [HttpPut]
        public bool Reset(string ProductToken, string UserCode)
        {
            try
            {
                return this.subscriptionsService.ResetSubscription(ProductToken, UserCode);
            }
            catch (Contract.Exceptions.ProductsNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Contract.Exceptions.UserNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Contract.Exceptions.UserInactiveException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error desconocido en el servidor"));
            }
        }

        // GET api/<controller
        [Route("{ProductToken}/{UserCode}")]
        public Subscriptions Get(string ProductToken, string UserCode)
        {
            try
            {
                return this.subscriptionsService.Get(ProductToken, UserCode);
            }
            catch (Contract.Exceptions.ProductsNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Contract.Exceptions.UserNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Contract.Exceptions.UserInactiveException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Contract.Exceptions.StandardProfileNotFoundException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                //throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error desconocido en el servidor"));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message+ex.StackTrace));
            }
        }
    }

    public class ResetData
    {
        public string ProductToken { get; set; }
        public string IdUserExternal { get; set; }
    }
}