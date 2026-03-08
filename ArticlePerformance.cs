using System;
using System.Collections.Generic;
using System.Linq;

namespace GestionComerce
{
    /// <summary>
    /// Combines data from Operations, OperationArticles, Invoices and Articles
    /// to provide aggregated article-level statistics for the reporting view (CMainR).
    /// </summary>
    public class ArticlePerformance
    {
        public int ArticleID { get; set; }
        public string ArticleName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ArticleDataAggregator
    {
        private readonly List<Operation> _operations;
        private readonly List<OperationArticle> _opArticles;
        private readonly List<Invoice> _invoices;
        private readonly List<Article> _articles;

        public ArticleDataAggregator(
            List<Operation> operations,
            List<OperationArticle> opArticles,
            List<Invoice> invoices,
            List<Article> articles)
        {
            _operations = operations ?? new List<Operation>();
            _opArticles = opArticles ?? new List<OperationArticle>();
            _invoices = invoices ?? new List<Invoice>();
            _articles = articles ?? new List<Article>();
        }

        /// <summary>
        /// Returns the top N best-selling articles by revenue in the given date range.
        /// Combines data from both OperationArticles and Invoice articles.
        /// </summary>
        public List<ArticlePerformance> GetTopSellingArticles(DateTime from, DateTime to, int topN = 5)
        {
            var performanceMap = new Dictionary<int, ArticlePerformance>();

            // ── Source 1: Operation + OperationArticle ─────────────────────────────
            var ventesInRange = new HashSet<int>(
                _operations
                    .Where(o => !o.Reversed
                             && o.OperationType != null
                             && o.OperationType.StartsWith("Vente", StringComparison.OrdinalIgnoreCase)
                             && o.DateOperation >= from
                             && o.DateOperation <= to)
                    .Select(o => o.OperationID));

            foreach (var oa in _opArticles.Where(oa => !oa.Reversed && ventesInRange.Contains(oa.OperationID)))
            {
                // Find article price from master list
                var art = _articles.FirstOrDefault(a => a.ArticleID == oa.ArticleID);
                decimal prix = art != null ? art.PrixVente : 0;
                string name = art != null ? (art.ArticleName ?? string.Format("Article #{0}", oa.ArticleID))
                                           : string.Format("Article #{0}", oa.ArticleID);

                if (!performanceMap.ContainsKey(oa.ArticleID))
                    performanceMap[oa.ArticleID] = new ArticlePerformance
                    {
                        ArticleID = oa.ArticleID,
                        ArticleName = name
                    };

                performanceMap[oa.ArticleID].TotalSold += oa.QteArticle;
                performanceMap[oa.ArticleID].Revenue += prix * oa.QteArticle;
            }

            // ── Source 2: Invoice articles ─────────────────────────────────────────
            foreach (var inv in _invoices.Where(i =>
                !i.IsReversed && !i.IsDeleted &&
                i.InvoiceDate >= from && i.InvoiceDate <= to &&
                (i.InvoiceType == null || !i.InvoiceType.ToLower().Contains("achat"))))
            {
                if (inv.Articles == null) continue;
                foreach (var ia in inv.Articles.Where(a => !a.IsReversed && !a.IsDeleted))
                {
                    int artId = ia.ArticleID;
                    string name = ia.ArticleName ?? string.Format("Article #{0}", artId);

                    if (!performanceMap.ContainsKey(artId))
                        performanceMap[artId] = new ArticlePerformance
                        {
                            ArticleID = artId,
                            ArticleName = name
                        };

                    performanceMap[artId].TotalSold += (int)ia.Quantite;
                    performanceMap[artId].Revenue += ia.PrixUnitaire * ia.Quantite;
                }
            }

            return performanceMap.Values
                .OrderByDescending(p => p.Revenue)
                .Take(topN)
                .ToList();
        }
    }
}