using Nop.Core;

namespace Nop.Plugin.Feed.Zbozi.Domain
{
    /// <summary>
    /// Represents a Zbozi product record
    /// </summary>
    public partial class ZboziProductRecord : BaseEntity
    {
        public int ProductId { get; set; }
        public string Taxonomy { get; set; }

        /// <summary>
        /// A value indicating whether it's custom goods (not unique identifier exists)
        /// </summary>
        public bool CustomGoods { get; set; }
        public string Gender { get; set; }
        public string AgeGroup { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public string Material { get; set; }
        public string Pattern { get; set; }
        public string ItemGroupId { get; set; }
		public string ProductName { get; set; }
		public string Params { get; set; }
		public string MAX_CPC { get; set; }
		public string MAX_CPC_SEARCH { get; set; }
		public string VIDEO_URL { get; set; }
		public string EXTRA_MESSAGE { get; set; }

	}
}