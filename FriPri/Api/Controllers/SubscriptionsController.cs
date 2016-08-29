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

        // GET api/<controller>
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
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error desconocido en el servidor"));
            }
        }
    }
}