using Autofac;
using Autofac.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Plugin.Feed.Zbozi.Data;
using Nop.Plugin.Feed.Zbozi.Domain;
using Nop.Plugin.Feed.Zbozi.Services;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Feed.Zbozi
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<ZboziService>().As<IZboziService>().InstancePerLifetimeScope();

            //data context
            this.RegisterPluginDataContext<ZboziProductObjectContext>(builder, "nop_object_context_zbozi_product");

            //override required repository with our custom context
            builder.RegisterType<EfRepository<ZboziProductRecord>>()
                .As<IRepository<ZboziProductRecord>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_zbozi_product"))
                .InstancePerLifetimeScope();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 1; }
        }
    }
}
