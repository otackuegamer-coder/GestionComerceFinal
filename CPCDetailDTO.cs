using System;
using System.Collections.Generic;

namespace GestionComerce
{
    /// <summary>
    /// Extended CPC DTO with Exploitation / Financier / Non-Courant breakdown.
    /// Add this class to your Models folder (e.g. CPCDetailDTO.cs).
    /// </summary>
    public class CPCDetailDTO
    {
        // ── Produits ──────────────────────────────────────────────────
        public List<CPCLigneDTO> ProduitsExploitation { get; set; } = new List<CPCLigneDTO>();
        public List<CPCLigneDTO> ProduitsFinanciers { get; set; } = new List<CPCLigneDTO>();
        public List<CPCLigneDTO> ProduitsNonCourants { get; set; } = new List<CPCLigneDTO>();

        public decimal TotalProduitsExploitation { get; set; }
        public decimal TotalProduitsFinanciers { get; set; }
        public decimal TotalProduitsNonCourants { get; set; }
        public decimal TotalProduits => TotalProduitsExploitation + TotalProduitsFinanciers + TotalProduitsNonCourants;

        // ── Charges ───────────────────────────────────────────────────
        public List<CPCLigneDTO> ChargesExploitation { get; set; } = new List<CPCLigneDTO>();
        public List<CPCLigneDTO> ChargesFinancieres { get; set; } = new List<CPCLigneDTO>();
        public List<CPCLigneDTO> ChargesNonCourantes { get; set; } = new List<CPCLigneDTO>();

        public decimal TotalChargesExploitation { get; set; }
        public decimal TotalChargesFinancieres { get; set; }
        public decimal TotalChargesNonCourantes { get; set; }
        public decimal TotalCharges => TotalChargesExploitation + TotalChargesFinancieres + TotalChargesNonCourantes;

        // ── Net Result ────────────────────────────────────────────────
        public decimal ResultatNet => TotalProduits - TotalCharges;

        // ── Period ────────────────────────────────────────────────────
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }
}