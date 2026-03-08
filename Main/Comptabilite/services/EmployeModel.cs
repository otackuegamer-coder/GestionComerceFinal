using System;

namespace Superete.Main.Comptabilite.Models
{
    public class EmployeModel
    {
        // Essential fields
        public int EmployeID { get; set; }
        public string NomComplet { get; set; }
        public string CIN { get; set; }
        public string CNSS { get; set; }
        public bool Actif { get; set; }

        // Salary
        public decimal SalaireBase { get; set; }

        // Optional fields (can be null)
        public DateTime? DateNaissance { get; set; }
        public string Telephone { get; set; }
        public string Email { get; set; }
        public string Adresse { get; set; }
        public string Poste { get; set; }
        public DateTime? DateEmbauche { get; set; }
        public string CreePar { get; set; }
        public DateTime? DateCreation { get; set; }
        public string ModifiePar { get; set; }
        public DateTime? DateModification { get; set; }

        // Display property for ComboBox
        public string DisplayText => $"{NomComplet} - {CIN}";
    }
}