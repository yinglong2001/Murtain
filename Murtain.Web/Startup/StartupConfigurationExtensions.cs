﻿using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Murtain.Caching;
using Murtain.Collections;
using Murtain.Configuration.Startup;
using Murtain.Dependency;
using Murtain.Dependency.ConventionalRegistrars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Compilation;
using System.Web.Http;
using System.Web.Mvc;

namespace Murtain.Configuration.Startup
{
    public static class StartupConfigurationExtensions
    {
        public static StartupConfiguration RegisterWebMvcApplication(this StartupConfiguration bootstrap, params IModule[] modules)
        {
            var assemblies = AssemblyLoader.FilterSystemAssembly(BuildManager.GetReferencedAssemblies().Cast<Assembly>());

            IocManager.Instance.AddConventionalRegistrar(new ControllerConventionalRegistrar());
            IocManager.Instance.RegisterAssemblyByConvention(assemblies, modules);

            DependencyResolver.SetResolver(new AutofacDependencyResolver(IocManager.Instance.Container));

            return bootstrap;
        }
        public static StartupConfiguration RegisterWebApiApplication(this StartupConfiguration bootstrap, params Autofac.Module[] modules)
        {
            var assemblies = AssemblyLoader.FilterSystemAssembly(BuildManager.GetReferencedAssemblies().Cast<Assembly>());

            HttpConfiguration configuration = GlobalConfiguration.Configuration;
            IocManager.Instance.AddConventionalRegistrar(new ApiControllerConventionalRegistrar());
            IocManager.Instance.RegisterAssemblyByConvention(assemblies, modules);

            configuration.DependencyResolver = new AutofacWebApiDependencyResolver(IocManager.Instance.Container);

            return bootstrap;
        }

    }
}
