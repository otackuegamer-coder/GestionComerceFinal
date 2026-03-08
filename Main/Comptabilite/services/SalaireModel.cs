using System;

namespace Superete.Main.Comptabilite.Models
{
    public class SalaireModel
    {
        public int SalaireID { get; set; }
        public int EmployeID { get; set; }
        public string NomComplet { get; set; }
        public string CIN { get; set; }
        public string CNSS { get; set; }

        public int Mois { get; set; }
        public int Annee { get; set; }
        public DateTime? DatePaiement { get; set; }

        public decimal SalaireBase { get; set; }
        public decimal HeuresNormales { get; set; }
        public decimal TauxHoraire { get; set; }

        public decimal PrimeAnciennete { get; set; }
        public decimal PrimeRendement { get; set; }
        public decimal PrimeResponsabilite { get; set; }
        public decimal IndemniteTransport { get; set; }
        public decimal IndemniteLogement { get; set; }
        public decimal AutresPrimes { get; set; }

        public decimal HeuresSupp25 { get; set; }
        public decimal HeuresSupp50 { get; set; }
        public decimal HeuresSupp100 { get; set; }
        public decimal MontantHeuresSupp { get; set; }

        public decimal SalaireBrut { get; set; }

        public decimal CotisationCNSS { get; set; }
        public decimal CotisationAMO { get; set; }
        public decimal CotisationCIMR { get; set; }
        public decimal MontantIR { get; set; }
        public decimal AvanceSurSalaire { get; set; }
        public decimal PretPersonnel { get; set; }
        public decimal Penalites { get; set; }
        public decimal AutresRetenues { get; set; }

        public decimal TotalRetenues { get; set; }
        public decimal SalaireNet { get; set; }

        public decimal CotisationPatronaleCNSS { get; set; }
        public decimal CotisationPatronaleAMO { get; set; }

        public string Statut { get; set; }
        public string Remarques { get; set; }

        public string CreePar { get; set; }
        public DateTime? DateCreation { get; set; }
        public string ModifiePar { get; set; }
        public DateTime? DateModification { get; set; }

        // Display property
        public string PeriodeDisplay => $"{Mois:00}/{Annee}";
        // Add inside SalaireModel class (computed, not stored)
        public decimal TotalPrimesDisplay =>
            PrimeAnciennete + PrimeRendement + PrimeResponsabilite
            + IndemniteTransport + IndemniteLogement + AutresPrimes;
    }
}