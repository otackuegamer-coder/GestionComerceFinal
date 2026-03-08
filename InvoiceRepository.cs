using System.Collections.Generic;
using System.Threading.Tasks;
using GestionComerce;

namespace GestionComerce.Main.Facturation
{
    /// <summary>
    /// Thin wrapper around GestionComerce.Invoice that provides the InvoiceRepository
    /// interface expected by WFacturePage, CMainHf, WEditInvoice, LivraisonDetailsWindow, etc.
    /// All real work is delegated to Invoice which calls the REST API.
    /// The constructor connection-string parameter is accepted but ignored (API is used instead).
    /// </summary>
    public class InvoiceRepository
    {
        private readonly Invoice _invoice = new Invoice();

        public InvoiceRepository(string connectionString)
        {
            // connectionString is ignored — the app now uses the REST API via Invoice
        }

        public Task<List<Invoice>> GetAllInvoicesAsync(bool includeDeleted = false)
        {
            return _invoice.GetAllInvoicesAsync(includeDeleted);
        }

        public Task<Invoice> GetInvoiceByIdAsync(int invoiceId)
        {
            return _invoice.GetInvoiceByIdAsync(invoiceId);
        }

        public Task<Invoice> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            return _invoice.GetInvoiceByNumberAsync(invoiceNumber);
        }

        public Task<List<Invoice>> SearchInvoicesAsync(string searchTerm,
            System.DateTime? startDate = null, System.DateTime? endDate = null)
        {
            return _invoice.SearchInvoicesAsync(searchTerm, startDate, endDate);
        }

        public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber)
        {
            return _invoice.InvoiceNumberExistsAsync(invoiceNumber);
        }

        public Task<int> CreateInvoiceAsync(Invoice invoice)
        {
            return _invoice.CreateInvoiceAsync(invoice);
        }

        public Task<bool> UpdateInvoiceAsync(Invoice invoice)
        {
            return _invoice.UpdateInvoiceAsync(invoice);
        }

        public Task<bool> DeleteInvoiceAsync(int invoiceId)
        {
            return _invoice.DeleteInvoiceAsync(invoiceId);
        }

        public Task<List<Invoice.InvoiceArticle>> GetInvoiceArticlesAsync(int invoiceId)
        {
            return _invoice.GetInvoiceArticlesAsync(invoiceId);
        }

        public Task<bool> AddInvoiceArticleAsync(Invoice.InvoiceArticle article)
        {
            return _invoice.AddInvoiceArticleAsync(article);
        }

        public Task<bool> UpdateInvoiceArticleAsync(Invoice.InvoiceArticle article)
        {
            return _invoice.UpdateInvoiceArticleAsync(article);
        }

        public Task<bool> DeleteInvoiceArticleAsync(int invoiceArticleId)
        {
            return _invoice.DeleteInvoiceArticleAsync(invoiceArticleId);
        }
    }
}