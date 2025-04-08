using One.Inception.AspNetCore.Exceptions;
using One.Inception.MessageProcessing;
using One.Inception.Multitenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace One.Inception.AspNet
{
    public static class InceptionAspNetExtensions
    {
        public static IServiceCollection AddInceptionAspNet(this IServiceCollection services)
        {
            services.AddSingleton<ITenantResolver<DefaultHttpContext>, HttpContextTenantResolver>();
            services.AddSingleton<ITenantResolver<HttpContext>, HttpContextTenantResolver>();
            services.AddSingleton<HttpContextTenantResolver>();

            return services;
        }

        public static IApplicationBuilder UseInceptionAspNet(this IApplicationBuilder app)
        {
            return app.Use((context, next) =>
            {
                bool shouldResolve = ShouldResolveTenant(context);
                if (shouldResolve)
                    return ResolveInceptionContext(context, next);

                return next.Invoke();
            });
        }

        public static IApplicationBuilder UseInceptionAspNet(this IApplicationBuilder app, Func<HttpContext, bool> shouldResolveTenant)
        {
            return app.Use((context, next) =>
            {
                bool shouldResolve = true;
                if (shouldResolveTenant is null == false)
                    shouldResolve = shouldResolveTenant(context);

                if (shouldResolve)
                {
                    return ResolveInceptionContext(context, next);
                }

                return next.Invoke();
            });
        }

        private static Task ResolveInceptionContext(HttpContext context, Func<Task> next)
        {
            try
            {
                var contextFactory = context.RequestServices.GetRequiredService<DefaultContextFactory>();
                InceptionContext inceptionContext = contextFactory.Create(context, context.RequestServices);

                ILogger logger = context.RequestServices.GetService<ILogger<InceptionContext>>();
                using (logger.BeginScope(s => s.AddScope(Log.Tenant, inceptionContext.Tenant)))
                {
                    return next.Invoke();
                }
            }
            catch (UnableToResolveTenantException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                return Task.CompletedTask;
            }
        }

        private static bool ShouldResolveTenant(HttpContext context)
        {
            bool shoudResolve = true;

            Endpoint endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
            if (endpoint is not null)
            {
                BypassTenantAttribute doNotRequireTenantAttribute = endpoint.Metadata.GetMetadata<BypassTenantAttribute>();
                if (doNotRequireTenantAttribute is not null)
                    shoudResolve = false;
            }
            return shoudResolve;
        }
    }
}
