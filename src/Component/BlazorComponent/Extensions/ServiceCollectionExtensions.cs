﻿using BlazorComponent;
using BlazorComponent.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorComponent(this IServiceCollection services)
        {
            services.TryAddScoped<DomEventJsInterop>();
            services.TryAddScoped<HeadJsInterop>();
            services.TryAddScoped<Document>();
            services.TryAddSingleton<IComponentIdGenerator, GuidComponentIdGenerator>();
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

            return services;
        }
    }
}
