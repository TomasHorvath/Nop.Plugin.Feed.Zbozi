using Nop.Data.Mapping;
using Nop.Plugin.Feed.Zbozi.Domain;

namespace Nop.Plugin.Feed.Zbozi.Data
{
    public partial class ZboziProductRecordMap : NopEntityTypeConfiguration<ZboziProductRecord>
    {
        public ZboziProductRecordMap()
        {
            this.ToTable("ZboziProduct");
            this.HasKey(x => x.Id);
        }
    }
}