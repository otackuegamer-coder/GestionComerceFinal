using System;
using System.Collections.Generic;
using System.Linq;
using Superete.Main.Comptabilite.Models;
using Superete.Main.Comptabilite.Services;

namespace GestionComerce.Main.ProjectManagment
{
    /// <summary>
    /// Helper class to integrate salary data into reports
    /// Add this to your MainWindow or create a service layer
    /// </summary>
    public class SalaryReportHelper
    {
        private readonly SalaireService salaireService;
        private readonly EmployeService employeService;

        public SalaryReportHelper()
        {
            string connectionString = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";
            salaireService = new SalaireService(connectionString);
            employeService = new EmployeService(connectionString);
        }

        /// <summary>
        /// Gets top 5 highest salaries for a given period
        /// </summary>
        public List<TopSalaryItem> GetTopSalaries(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                List<SalaireModel> allSalaries;

                if (startDate.HasValue && endDate.HasValue)
                {
                    // Get all salaries and filter by date range
                    allSalaries = salaireService.GetAll()
                        .Where(s => IsInDateRange(s, startDate.Value, endDate.Value))
                        .ToList();
                }
                else
                {
                    // Get all salaries
                    allSalaries = salaireService.GetAll();
                }

                // Group by employee and sum salaries
                var employeeSalaries = allSalaries
                    .Where(s => s.Statut == "Payé") // Only paid salaries
                    .GroupBy(s => s.EmployeID)
                    .Select(g => new
                    {
                        EmployeID = g.Key,
                        NomComplet = g.First().NomComplet,
                        TotalSalaire = g.Sum(s => s.SalaireNet),
                        NombrePaiements = g.Count()
                    })
                    .OrderByDescending(x => x.TotalSalaire)
                    .Take(5)
                    .ToList();

                return employeeSalaries.Select(e => new TopSalaryItem
                {
                    EmployeID = e.EmployeID,
                    EmployeeName = e.NomComplet,
                    TotalSalary = e.TotalSalaire,
                    PaymentCount = e.NombrePaiements
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du chargement des salaires: {ex.Message}");
                return new List<TopSalaryItem>();
            }
        }

        /// <summary>
        /// Gets highest single salary payment for a given period
        /// </summary>
        public List<TopSalaryItem> GetTopSingleSalaries(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                List<SalaireModel> allSalaries;

                if (startDate.HasValue && endDate.HasValue)
                {
                    allSalaries = salaireService.GetAll()
                        .Where(s => IsInDateRange(s, startDate.Value, endDate.Value))
                        .ToList();
                }
                else
                {
                    allSalaries = salaireService.GetAll();
                }

                var topSalaries = allSalaries
                    .Where(s => s.Statut == "Payé")
                    .OrderByDescending(s => s.SalaireNet)
                    .Take(5)
                    .Select(s => new TopSalaryItem
                    {
                        EmployeID = s.EmployeID,
                        EmployeeName = s.NomComplet,
                        TotalSalary = s.SalaireNet,
                        PaymentCount = 1,
                        Period = $"{s.Mois:00}/{s.Annee}"
                    })
                    .ToList();

                return topSalaries;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors du chargement des salaires: {ex.Message}");
                return new List<TopSalaryItem>();
            }
        }

        private bool IsInDateRange(SalaireModel salaire, DateTime startDate, DateTime endDate)
        {
            // Create a date from the salary month/year (use first day of month)
            var salaireDate = new DateTime(salaire.Annee, salaire.Mois, 1);

            // Check if the salary month falls within the date range
            return salaireDate >= new DateTime(startDate.Year, startDate.Month, 1) &&
                   salaireDate < new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
        }
    }

    /// <summary>
    /// Data model for top salary items
    /// </summary>
    public class TopSalaryItem
    {
        public int EmployeID { get; set; }
        public string EmployeeName { get; set; }
        public decimal TotalSalary { get; set; }
        public int PaymentCount { get; set; }
        public string Period { get; set; }
    }
}
