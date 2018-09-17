﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Security.Claims;
using System.Web;

namespace Hydra.Such.Portal.Filters
{

    public class NavigationFilter : ActionFilterAttribute
    {
        /*
        public override void OnActionExecuted(ActionExecutedContext context)
        {


            var cenas = (Controller)context.Controller;
            //cenas.User;

            var httpMethod = context.HttpContext.Request.Method;
            //var user = context.HttpContext.User;            


            if (httpMethod == "GET" && user.Identity.IsAuthenticated && !user.HasClaim(c => c.Type == "menu"))
            {
                // get menu
                // if menu assign
                // else define empty menu and assign

                ((ClaimsIdentity)user.Identity).AddClaim(new Claim("menu", "teste"));

                ((ClaimsIdentity)user.Identity).BootstrapContext = ((ClaimsIdentity)user.Identity).BootstrapContext;

                user.AddIdentity((ClaimsIdentity)user.Identity);

                var tres = 2;

            }

        }*/

        public override void OnActionExecuting(ActionExecutingContext context)
        {

            var httpMethod = context.HttpContext.Request.Method;
            var user = context.HttpContext.User;
            var session = context.HttpContext.Session;

            if (httpMethod == "GET" && user.Identity.IsAuthenticated && session.GetString("menu") != null)
            {
                // get menu
                // if menu assign
                // else define empty menu and assign

                session.SetString("menu", "teste");
            }
            base.OnActionExecuting(context);
        }

    }
}