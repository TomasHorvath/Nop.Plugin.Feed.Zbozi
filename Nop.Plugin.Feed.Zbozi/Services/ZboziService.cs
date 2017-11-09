using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nop.Core.Data;
using Nop.Plugin.Feed.Zbozi.Domain;

namespace Nop.Plugin.Feed.Zbozi.Services
{
    public partial class ZboziService : IZboziService
    {
        #region Fields

        private readonly IRepository<ZboziProductRecord> _zboziProductsRepository;

        #endregion

        #region Ctor

        public ZboziService(IRepository<ZboziProductRecord> gpRepository)
        {
            this._zboziProductsRepository = gpRepository;
        }

        #endregion

        #region Utilities

        private string GetEmbeddedFileContent(string resourceName)
        {
            string fullResourceName = string.Format("Nop.Plugin.Feed.Zbozi.Files.{0}", resourceName);
            var assem = this.GetType().Assembly;
            using (var stream = assem.GetManifestResourceStream(fullResourceName))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        #endregion

        #region Methods

        public virtual void DeleteGoogleProduct(ZboziProductRecord googleProductRecord)
        {
            if (googleProductRecord == null)
                throw new ArgumentNullException("zboziProductRecord");

            _zboziProductsRepository.Delete(googleProductRecord);
        }

        public virtual IList<ZboziProductRecord> GetAll()
        {
            var query = from gp in _zboziProductsRepository.Table
                        orderby gp.Id
                        select gp;
            var records = query.ToList();
            return records;
        }

        public virtual ZboziProductRecord GetById(int googleProductRecordId)
        {
            if (googleProductRecordId == 0)
                return null;

            return _zboziProductsRepository.GetById(googleProductRecordId);
        }

        public virtual ZboziProductRecord GetByProductId(int productId)
        {
            if (productId == 0)
                return null;

            var query = from gp in _zboziProductsRepository.Table
                        where gp.ProductId == productId
                        orderby gp.Id
                        select gp;
            var record = query.FirstOrDefault();
            return record;
        }

        public virtual void InsertGoogleProductRecord(ZboziProductRecord zboziProductRecord)
        {
            if (zboziProductRecord == null)
                throw new ArgumentNullException("googleProductRecord");

            _zboziProductsRepository.Insert(zboziProductRecord);
        }

        public virtual void UpdateGoogleProductRecord(ZboziProductRecord zboziProductRecord)
        {
            if (zboziProductRecord == null)
                throw new ArgumentNullException("googleProductRecord");

            _zboziProductsRepository.Update(zboziProductRecord);
        }

        public virtual IList<string> GetTaxonomyList()
        {
            var fileContent = GetEmbeddedFileContent("taxonomy.txt");
            if (String.IsNullOrEmpty((fileContent)))
                return new List<string>();

            //parse the file
            var result = fileContent.Split(new [] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries)

                .ToList();
            return result;
        }

        #endregion
    }
}
