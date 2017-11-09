using System.Collections.Generic;
using Nop.Plugin.Feed.Zbozi.Domain;

namespace Nop.Plugin.Feed.Zbozi.Services
{
    public partial interface IZboziService
    {
        void DeleteGoogleProduct(ZboziProductRecord zboziProductRecord);

        IList<ZboziProductRecord> GetAll();

		ZboziProductRecord GetById(int googleProductRecordId);

		ZboziProductRecord GetByProductId(int productId);

        void InsertGoogleProductRecord(ZboziProductRecord zboziProductRecord);

        void UpdateGoogleProductRecord(ZboziProductRecord zboziProductRecord);

        IList<string> GetTaxonomyList();
    }
}
