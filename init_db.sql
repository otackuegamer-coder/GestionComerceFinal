-- =============================================================================
-- Zenix Gestion Commerciale - Database Initialisation Script
-- Idempotent: safe to run multiple times.
-- =============================================================================

USE master;
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GESTIONCOMERCEP')
    CREATE DATABASE GESTIONCOMERCEP;
GO
USE GESTIONCOMERCEP;
GO

-- ── TABLES ───────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Role')
CREATE TABLE [Role] (
    [RoleID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [RoleName] NVARCHAR(100) NOT NULL,
    [Etat] BIT NULL,
    [CreateClient] BIT NOT NULL DEFAULT 0, [ModifyClient] BIT NOT NULL DEFAULT 0,
    [DeleteClient] BIT NOT NULL DEFAULT 0, [ViewOperationClient] BIT NOT NULL DEFAULT 0,
    [PayeClient] BIT NOT NULL DEFAULT 0,   [ViewClient] BIT NOT NULL DEFAULT 0,
    [CreateFournisseur] BIT NOT NULL DEFAULT 0, [ModifyFournisseur] BIT NOT NULL DEFAULT 0,
    [DeleteFournisseur] BIT NOT NULL DEFAULT 0, [ViewOperationFournisseur] BIT NOT NULL DEFAULT 0,
    [PayeFournisseur] BIT NOT NULL DEFAULT 0, [ViewFournisseur] BIT NOT NULL DEFAULT 0,
    [ReverseOperation] BIT NOT NULL DEFAULT 0, [ReverseMouvment] BIT NOT NULL DEFAULT 0,
    [ViewOperation] BIT NOT NULL DEFAULT 0, [ViewMouvment] BIT NOT NULL DEFAULT 0,
    [ViewProjectManagment] BIT NOT NULL DEFAULT 0, [ViewSettings] BIT NOT NULL DEFAULT 0,
    [ViewUsers] BIT NOT NULL DEFAULT 0, [EditUsers] BIT NOT NULL DEFAULT 0,
    [DeleteUsers] BIT NOT NULL DEFAULT 0, [AddUsers] BIT NOT NULL DEFAULT 0,
    [ViewRoles] BIT NOT NULL DEFAULT 0, [AddRoles] BIT NOT NULL DEFAULT 0,
    [DeleteRoles] BIT NOT NULL DEFAULT 0,
    [ViewFamilly] BIT NOT NULL DEFAULT 0, [EditFamilly] BIT NOT NULL DEFAULT 0,
    [DeleteFamilly] BIT NOT NULL DEFAULT 0, [AddFamilly] BIT NOT NULL DEFAULT 0,
    [AddArticle] BIT NULL DEFAULT 0, [DeleteArticle] BIT NULL DEFAULT 0,
    [EditArticle] BIT NULL DEFAULT 0, [ViewArticle] BIT NULL DEFAULT 0,
    [Repport] BIT NULL DEFAULT 0, [Ticket] BIT NULL DEFAULT 0,
    [SolderFournisseur] BIT NULL DEFAULT 0, [SolderClient] BIT NULL DEFAULT 0,
    [ViewFactureSettings] BIT NULL DEFAULT 0, [ModifyFactureSettings] BIT NULL DEFAULT 0,
    [ViewFacture] BIT NULL DEFAULT 0,
    [ViewPaymentMethod] BIT NULL DEFAULT 0, [AddPaymentMethod] BIT NULL DEFAULT 0,
    [ModifyPaymentMethod] BIT NULL DEFAULT 0, [DeletePaymentMethod] BIT NULL DEFAULT 0,
    [ViewApropos] BIT NULL DEFAULT 0, [Logout] BIT NULL DEFAULT 0,
    [ViewExit] BIT NULL DEFAULT 0, [ViewShutDown] BIT NULL DEFAULT 0,
    [ViewClientsPage] BIT NULL DEFAULT 0, [ViewFournisseurPage] BIT NULL DEFAULT 0,
    [ViewInventrory] BIT NULL DEFAULT 0, [ViewVente] BIT NULL DEFAULT 0,
    [CashClient] BIT NULL DEFAULT 0, [ViewCreditClient] BIT NULL DEFAULT 0,
    [ViewCreditFournisseur] BIT NULL DEFAULT 0, [CashFournisseur] BIT NULL DEFAULT 0,
    [AccessFacturation] BIT NULL DEFAULT 0, [CreateFacture] BIT NULL DEFAULT 0,
    [HistoriqueFacture] BIT NULL DEFAULT 0, [HistoryCheck] BIT NULL DEFAULT 0,
    [FactureEnregistrees] BIT NULL DEFAULT 0,
    [AccessLivraison] BIT NULL DEFAULT 0, [CreationLivraison] BIT NULL DEFAULT 0,
    [GestionLivreur] BIT NULL DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
CREATE TABLE [Users] (
    [UserID]   INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(100) NOT NULL,
    [Code]     NVARCHAR(50)  NOT NULL,
    [RoleID]   INT           NULL,
    [Etat]     BIT           NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Familly')
CREATE TABLE [Familly] (
    [FamilleID]    INT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FamillyName]  VARCHAR(200) NULL,
    [NbrArticles]  INT          NULL,
    [Etat]         BIT          NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Article')
CREATE TABLE [Article] (
    [ArticleID]            INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Quantite]             INT           NOT NULL,
    [PrixAchat]            DECIMAL(10,2) NOT NULL,
    [PrixVente]            DECIMAL(10,2) NOT NULL,
    [PrixMP]               DECIMAL(10,2) NULL,
    [FamillyID]            INT           NOT NULL,
    [Code]                 BIGINT        NULL,
    [ArticleName]          VARCHAR(MAX)  NULL,
    [Etat]                 BIT           NULL,
    [FournisseurID]        INT           NULL,
    [DateExpiration]       DATE          NULL,
    [BonLivraison]         NVARCHAR(100) NULL,
    [NumeroLot]            NVARCHAR(50)  NULL,
    [TVA]                  DECIMAL(5,2)  NULL,
    [DateLivraison]        DATE          NULL,
    [Marque]               NVARCHAR(100) NULL,
    [Date]                 DATE          NOT NULL,
    [ArticleImage]         VARBINARY(MAX) NULL,
    [IsUnlimitedStock]     BIT           NOT NULL DEFAULT 0,
    [PiecesPerPackage]     INT           NULL,
    [PackageType]          NVARCHAR(50)  NULL,
    [PackageWeight]        DECIMAL(18,3) NULL,
    [PackageDimensions]    NVARCHAR(100) NULL,
    [MinimumStock]         INT           NULL,
    [MaximumStock]         INT           NULL,
    [StorageLocation]      NVARCHAR(100) NULL,
    [SKU]                  NVARCHAR(50)  NULL,
    [Description]          NVARCHAR(MAX) NULL,
    [IsPerishable]         BIT           NULL,
    [UnitOfMeasure]        NVARCHAR(20)  NULL,
    [PrixGros]             DECIMAL(18,2) NULL,
    [MinQuantityForGros]   INT           NULL,
    [CountryOfOrigin]      NVARCHAR(100) NULL,
    [Manufacturer]         NVARCHAR(200) NULL,
    [LastRestockDate]      DATETIME      NULL,
    [Notes]                NVARCHAR(MAX) NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Client')
CREATE TABLE [Client] (
    [ClientID]         INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Nom]              NVARCHAR(100) NOT NULL,
    [Telephone]        NVARCHAR(20)  NULL,
    [Etat]             BIT           NOT NULL,
    [IsCompany]        BIT           NULL,
    [EtatJuridique]    NVARCHAR(100) NULL,
    [ICE]              NVARCHAR(50)  NULL,
    [SiegeEntreprise]  NVARCHAR(200) NULL,
    [Code]             NVARCHAR(50)  NULL,
    [Adresse]          NVARCHAR(200) NULL,
    [Remise]           DECIMAL(5,2)  NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Fournisseur')
CREATE TABLE [Fournisseur] (
    [FournisseurID]    INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Nom]              NVARCHAR(100) NOT NULL,
    [Telephone]        NVARCHAR(20)  NULL,
    [Etat]             BIT           NOT NULL,
    [EtatJuridic]      NVARCHAR(100) NULL,
    [ICE]              NVARCHAR(50)  NULL,
    [SiegeEntreprise]  NVARCHAR(150) NULL,
    [Adresse]          NVARCHAR(200) NULL,
    [Code]             NVARCHAR(50)  NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PaymentMethod')
CREATE TABLE [PaymentMethod] (
    [PaymentMethodID]   INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PaymentMethodName] NVARCHAR(100) NOT NULL,
    [Etat]              BIT           NOT NULL,
    [ImagePath]         NVARCHAR(MAX) NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Credit')
CREATE TABLE [Credit] (
    [CreditID]       INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ClientID]       INT           NULL,
    [Total]          DECIMAL(18,2) NOT NULL,
    [Paye]           DECIMAL(18,2) NOT NULL,
    [Difference]     DECIMAL(19,2) NULL,
    [Etat]           BIT           NOT NULL,
    [FournisseurID]  INT           NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Operation')
CREATE TABLE [Operation] (
    [OperationID]      INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Date]             DATETIME      NOT NULL,
    [PrixOperation]    DECIMAL(18,2) NOT NULL,
    [Remise]           DECIMAL(18,2) NULL,
    [CreditValue]      DECIMAL(18,2) NULL,
    [UserID]           INT           NOT NULL,
    [ClientID]         INT           NULL,
    [CreditID]         INT           NULL,
    [Etat]             BIT           NOT NULL,
    [FournisseurID]    INT           NULL,
    [OperationType]    NVARCHAR(50)  NULL,
    [Reversed]         BIT           NOT NULL DEFAULT 0,
    [PaymentMethodID]  INT           NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OperationArticle')
CREATE TABLE [OperationArticle] (
    [OperationArticleID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ArticleID]          INT NOT NULL,
    [OperationID]        INT NOT NULL,
    [QteArticle]         INT NOT NULL,
    [Etat]               BIT NULL,
    [Reversed]           BIT NOT NULL DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Facture')
CREATE TABLE [Facture] (
    [FactureID]         INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name]              NVARCHAR(100) NOT NULL,
    [ICE]               NVARCHAR(20)  NULL,
    [Telephone]         NVARCHAR(20)  NULL,
    [Adresse]           NVARCHAR(200) NULL,
    [Etat]              BIT           NULL,
    [VAT]               NVARCHAR(50)  NULL,
    [CompanyId]         NVARCHAR(50)  NULL,
    [EtatJuridic]       NVARCHAR(100) NULL,
    [SiegeEntreprise]   NVARCHAR(255) NULL,
    [LogoPath]          NVARCHAR(MAX) NULL,
    [IF]                NVARCHAR(50)  NULL,
    [CNSS]              NVARCHAR(50)  NULL,
    [RC]                NVARCHAR(50)  NULL,
    [TP]                NVARCHAR(50)  NULL,
    [RIB]               NVARCHAR(24)  NULL,
    [Email]             NVARCHAR(100) NULL,
    [SiteWeb]           NVARCHAR(200) NULL,
    [Patente]           NVARCHAR(50)  NULL,
    [Capital]           NVARCHAR(50)  NULL,
    [Fax]               NVARCHAR(20)  NULL,
    [Ville]             NVARCHAR(100) NULL,
    [CodePostal]        NVARCHAR(10)  NULL,
    [BankName]          NVARCHAR(100) NULL,
    [AgencyCode]        NVARCHAR(50)  NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FactureSettings')
CREATE TABLE [FactureSettings] (
    [FactureSettingsID]   INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CompanyName]         NVARCHAR(200) NOT NULL,
    [CompanyAddress]      NVARCHAR(500) NULL,
    [CompanyPhone]        NVARCHAR(50)  NULL,
    [CompanyEmail]        NVARCHAR(100) NULL,
    [LogoPath]            NVARCHAR(500) NULL,
    [InvoicePrefix]       NVARCHAR(20)  NULL,
    [TaxPercentage]       DECIMAL(5,2)  NULL,
    [TermsAndConditions]  NVARCHAR(MAX) NULL,
    [FooterText]          NVARCHAR(500) NULL,
    [CreatedDate]         DATETIME      NULL,
    [UpdatedDate]         DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Invoice')
CREATE TABLE [Invoice] (
    [InvoiceID]              INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [InvoiceNumber]          NVARCHAR(100) NOT NULL,
    [InvoiceDate]            DATE          NOT NULL,
    [InvoiceType]            NVARCHAR(50)  NOT NULL,
    [InvoiceIndex]           NVARCHAR(50)  NULL,
    [Objet]                  NVARCHAR(500) NULL,
    [NumberLetters]          NVARCHAR(500) NULL,
    [NameFactureGiven]       NVARCHAR(200) NULL,
    [NameFactureReceiver]    NVARCHAR(200) NULL,
    [ReferenceClient]        NVARCHAR(100) NULL,
    [UserName]               NVARCHAR(200) NULL,
    [UserICE]                NVARCHAR(50)  NULL,
    [UserVAT]                NVARCHAR(50)  NULL,
    [UserPhone]              NVARCHAR(50)  NULL,
    [UserAddress]            NVARCHAR(500) NULL,
    [UserEtatJuridique]      NVARCHAR(100) NULL,
    [UserIdSociete]          NVARCHAR(100) NULL,
    [UserSiegeEntreprise]    NVARCHAR(200) NULL,
    [ClientName]             NVARCHAR(200) NULL,
    [ClientICE]              NVARCHAR(50)  NULL,
    [ClientVAT]              NVARCHAR(50)  NULL,
    [ClientPhone]            NVARCHAR(50)  NULL,
    [ClientAddress]          NVARCHAR(500) NULL,
    [ClientEtatJuridique]    NVARCHAR(100) NULL,
    [ClientIdSociete]        NVARCHAR(100) NULL,
    [ClientSiegeEntreprise]  NVARCHAR(200) NULL,
    [Currency]               NVARCHAR(10)  NULL,
    [TVARate]                DECIMAL(5,2)  NULL,
    [TotalHT]                DECIMAL(18,2) NULL,
    [TotalTVA]               DECIMAL(18,2) NULL,
    [TotalTTC]               DECIMAL(18,2) NULL,
    [Remise]                 DECIMAL(18,2) NULL,
    [TotalAfterRemise]       DECIMAL(18,2) NULL,
    [EtatFacture]            INT           NULL,
    [IsReversed]             BIT           NULL,
    [Description]            NVARCHAR(MAX) NULL,
    [LogoPath]               NVARCHAR(500) NULL,
    [CreatedDate]            DATETIME      NULL,
    [CreatedBy]              INT           NULL,
    [ModifiedDate]           DATETIME      NULL,
    [ModifiedBy]             INT           NULL,
    [IsDeleted]              BIT           NULL,
    [CreditClientName]       NVARCHAR(255) NULL,
    [CreditMontant]          NVARCHAR(50)  NULL,
    [IsTransformed]          BIT           NOT NULL DEFAULT 0,
    [TransformedToId]        INT           NULL,
    [TransformedFromId]      INT           NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InvoiceArticle')
CREATE TABLE [InvoiceArticle] (
    [InvoiceArticleID] INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [InvoiceID]        INT           NOT NULL,
    [OperationID]      INT           NULL,
    [ArticleID]        INT           NOT NULL,
    [ArticleName]      NVARCHAR(200) NOT NULL,
    [PrixUnitaire]     DECIMAL(18,2) NOT NULL,
    [Quantite]         DECIMAL(18,3) NOT NULL,
    [TVA]              DECIMAL(5,2)  NULL,
    [TotalHT]          DECIMAL(37,5) NULL,
    [MontantTVA]       DECIMAL(38,6) NULL,
    [TotalTTC]         DECIMAL(38,6) NULL,
    [IsReversed]       BIT           NULL,
    [CreatedDate]      DATETIME      NULL,
    [IsDeleted]        BIT           NULL,
    [Remise]           DECIMAL(18,2) NOT NULL DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SavedInvoices')
CREATE TABLE [SavedInvoices] (
    [SavedInvoiceID]    INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [InvoiceImage]      VARBINARY(MAX) NULL,
    [ImageFileName]     NVARCHAR(255) NULL,
    [FournisseurID]     INT           NOT NULL,
    [TotalAmount]       DECIMAL(18,2) NOT NULL,
    [InvoiceDate]       DATE          NOT NULL,
    [Description]       NVARCHAR(1000) NULL,
    [InvoiceReference]  NVARCHAR(100) NULL,
    [Notes]             NVARCHAR(500) NULL,
    [CreatedDate]       DATETIME      NULL,
    [UpdatedDate]       DATETIME      NULL,
    [OperationId]       INT           NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SavedInvoicesArticles')
CREATE TABLE [SavedInvoicesArticles] (
    [SavedInvoiceArticleId] INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [SavedInvoiceId]        INT           NOT NULL,
    [ArticleId]             INT           NULL,
    [ArticleName]           NVARCHAR(255) NOT NULL,
    [PrixUnitaire]          DECIMAL(18,2) NOT NULL,
    [Quantite]              DECIMAL(18,4) NOT NULL,
    [Tva]                   DECIMAL(5,2)  NOT NULL,
    [IsReversed]            BIT           NOT NULL DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CheckHistory')
CREATE TABLE [CheckHistory] (
    [CheckID]          INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CheckReference]   NVARCHAR(100) NULL,
    [CheckImage]       VARBINARY(MAX) NULL,
    [CheckImagePath]   NVARCHAR(500) NULL,
    [InvoiceID]        INT           NULL,
    [CheckAmount]      DECIMAL(18,2) NULL,
    [CheckDate]        DATE          NOT NULL,
    [BankName]         NVARCHAR(200) NULL,
    [CheckStatus]      NVARCHAR(50)  NULL,
    [Notes]            NVARCHAR(1000) NULL,
    [CreatedDate]      DATETIME      NULL,
    [UpdatedDate]      DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Livraison')
CREATE TABLE [Livraison] (
    [LivraisonID]             INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [OperationID]             INT           NULL,
    [ClientID]                INT           NULL,
    [ClientNom]               NVARCHAR(255) NOT NULL DEFAULT '',
    [ClientTelephone]         NVARCHAR(50)  NOT NULL DEFAULT '',
    [AdresseLivraison]        NVARCHAR(MAX) NOT NULL DEFAULT '',
    [Ville]                   NVARCHAR(100) NULL,
    [CodePostal]              NVARCHAR(20)  NULL,
    [ZoneLivraison]           NVARCHAR(100) NULL,
    [FraisLivraison]          DECIMAL(10,2) NULL DEFAULT 0,
    [DateCommande]            DATETIME      NULL DEFAULT GETDATE(),
    [DateLivraisonPrevue]     DATETIME      NULL,
    [DateLivraisonEffective]  DATETIME      NULL,
    [LivreurID]               INT           NULL,
    [LivreurNom]              NVARCHAR(255) NULL,
    [Statut]                  NVARCHAR(50)  NULL DEFAULT 'en_attente',
    [Notes]                   NVARCHAR(MAX) NULL,
    [TotalCommande]           DECIMAL(10,2) NOT NULL DEFAULT 0,
    [ModePaiement]            NVARCHAR(50)  NULL,
    [PaiementStatut]          NVARCHAR(50)  NULL DEFAULT 'non_paye',
    [Etat]                    BIT           NULL DEFAULT 1,
    [DateCreation]            DATETIME      NULL DEFAULT GETDATE()
);
GO

-- Idempotent migrations for Livraison (handles databases created before these columns were added)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name='Livraison')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='Etat')
        ALTER TABLE [Livraison] ADD [Etat] BIT NULL DEFAULT 1;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='DateCreation')
        ALTER TABLE [Livraison] ADD [DateCreation] DATETIME NULL DEFAULT GETDATE();
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='DateCommande')
        ALTER TABLE [Livraison] ADD [DateCommande] DATETIME NULL DEFAULT GETDATE();
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='PaiementStatut')
        ALTER TABLE [Livraison] ADD [PaiementStatut] NVARCHAR(50) NULL DEFAULT 'non_paye';
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='TotalCommande')
        ALTER TABLE [Livraison] ADD [TotalCommande] DECIMAL(10,2) NOT NULL DEFAULT 0;
    -- Update existing rows so Etat=1 (active) where it was null
    UPDATE [Livraison] SET [Etat]=1 WHERE [Etat] IS NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LivraisonHistorique')
CREATE TABLE [LivraisonHistorique] (
    [HistoriqueID]   INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [LivraisonID]    INT           NOT NULL,
    [AncienStatut]   NVARCHAR(50)  NULL,
    [NouveauStatut]  NVARCHAR(50)  NOT NULL,
    [Commentaire]    NVARCHAR(MAX) NULL,
    [UserID]         INT           NULL,
    [DateChangement] DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Livreur')
CREATE TABLE [Livreur] (
    [LivreurID]               INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Nom]                     NVARCHAR(255) NOT NULL,
    [Prenom]                  NVARCHAR(255) NOT NULL,
    [Telephone]               NVARCHAR(50)  NOT NULL,
    [Email]                   NVARCHAR(255) NULL,
    [VehiculeType]            NVARCHAR(100) NULL,
    [VehiculeImmatriculation] NVARCHAR(50)  NULL,
    [Statut]                  NVARCHAR(50)  NULL,
    [ZoneCouverture]          NVARCHAR(MAX) NULL,
    [Photo]                   NVARCHAR(255) NULL,
    [DateEmbauche]            DATE          NULL,
    [Actif]                   BIT           NULL,
    [DateCreation]            DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ZoneLivraison')
CREATE TABLE [ZoneLivraison] (
    [ZoneID]              INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [NomZone]             NVARCHAR(100) NOT NULL,
    [Description]         NVARCHAR(MAX) NULL,
    [TarifBase]           DECIMAL(10,2) NOT NULL,
    [DelaiLivraisonMin]   INT           NULL,
    [Actif]               BIT           NULL,
    [DateCreation]        DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Employes')
CREATE TABLE [Employes] (
    [EmployeID]         INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [NomComplet]        NVARCHAR(200) NOT NULL,
    [CIN]               NVARCHAR(20)  NULL,
    [CNSS]              NVARCHAR(20)  NULL,
    [DateNaissance]     DATE          NULL,
    [Telephone]         NVARCHAR(20)  NULL,
    [Email]             NVARCHAR(100) NULL,
    [Adresse]           NVARCHAR(500) NULL,
    [Poste]             NVARCHAR(100) NULL,
    [DateEmbauche]      DATE          NULL,
    [Actif]             BIT           NULL,
    [CreePar]           NVARCHAR(100) NULL,
    [DateCreation]      DATETIME      NULL,
    [ModifiePar]        NVARCHAR(100) NULL,
    [DateModification]  DATETIME      NULL,
    [SalaireBase]       DECIMAL(18,2) NOT NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Salaires')
CREATE TABLE [Salaires] (
    [SalaireID]                  INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [EmployeID]                  INT           NOT NULL,
    [NomComplet]                 NVARCHAR(200) NOT NULL,
    [CIN]                        NVARCHAR(20)  NULL,
    [CNSS]                       NVARCHAR(20)  NULL,
    [Mois]                       INT           NOT NULL,
    [Annee]                      INT           NOT NULL,
    [DatePaiement]               DATE          NULL,
    [SalaireBase]                DECIMAL(18,2) NOT NULL,
    [HeuresNormales]             DECIMAL(8,2)  NULL,
    [TauxHoraire]                DECIMAL(18,2) NULL,
    [PrimeAnciennete]            DECIMAL(18,2) NULL,
    [PrimeRendement]             DECIMAL(18,2) NULL,
    [PrimeResponsabilite]        DECIMAL(18,2) NULL,
    [IndemniteTransport]         DECIMAL(18,2) NULL,
    [IndemniteLogement]          DECIMAL(18,2) NULL,
    [AutresPrimes]               DECIMAL(18,2) NULL,
    [HeuresSupp25]               DECIMAL(8,2)  NULL,
    [HeuresSupp50]               DECIMAL(8,2)  NULL,
    [HeuresSupp100]              DECIMAL(8,2)  NULL,
    [MontantHeuresSupp]          DECIMAL(18,2) NULL,
    [SalaireBrut]                DECIMAL(25,2) NULL,
    [CotisationCNSS]             DECIMAL(18,2) NULL,
    [CotisationAMO]              DECIMAL(18,2) NULL,
    [CotisationCIMR]             DECIMAL(18,2) NULL,
    [MontantIR]                  DECIMAL(18,2) NULL,
    [AvanceSurSalaire]           DECIMAL(18,2) NULL,
    [PretPersonnel]              DECIMAL(18,2) NULL,
    [Penalites]                  DECIMAL(18,2) NULL,
    [AutresRetenues]             DECIMAL(18,2) NULL,
    [TotalRetenues]              DECIMAL(25,2) NULL,
    [SalaireNet]                 DECIMAL(33,2) NULL,
    [CotisationPatronaleCNSS]    DECIMAL(18,2) NULL,
    [CotisationPatronaleAMO]     DECIMAL(18,2) NULL,
    [Statut]                     NVARCHAR(50)  NULL,
    [Remarques]                  NVARCHAR(500) NULL,
    [CreePar]                    NVARCHAR(100) NULL,
    [DateCreation]               DATETIME      NULL,
    [ModifiePar]                 NVARCHAR(100) NULL,
    [DateModification]           DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ParametresGeneraux')
CREATE TABLE [ParametresGeneraux] (
    [Id]                              INT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId]                          INT          NOT NULL,
    [AfficherClavier]                 NVARCHAR(50) NOT NULL,
    [MasquerEtiquettesVides]          BIT          NOT NULL,
    [SupprimerArticlesQuantiteZero]   BIT          NOT NULL,
    [Langue]                          NVARCHAR(50) NOT NULL,
    [ImprimerFactureParDefaut]        BIT          NOT NULL,
    [ImprimerTicketParDefaut]         BIT          NOT NULL,
    [MethodePaiementParDefaut]        NVARCHAR(50) NOT NULL,
    [DateCreation]                    DATETIME     NOT NULL,
    [DateModification]                DATETIME     NOT NULL,
    [VueParDefaut]                    NVARCHAR(50) NOT NULL,
    [TrierParDefaut]                  NVARCHAR(50) NOT NULL,
    [TailleIcones]                    NVARCHAR(50) NOT NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseCategories')
CREATE TABLE [ExpenseCategories] (
    [CategoryID]   INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CategoryName] NVARCHAR(50)  NOT NULL,
    [Description]  NVARCHAR(200) NULL,
    [IsActive]     BIT           NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Expenses')
CREATE TABLE [Expenses] (
    [ExpenseID]      INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ExpenseName]    NVARCHAR(100) NOT NULL,
    [Category]       NVARCHAR(50)  NOT NULL,
    [Amount]         DECIMAL(18,2) NOT NULL,
    [DueDate]        DATE          NOT NULL,
    [PaymentStatus]  NVARCHAR(20)  NULL,
    [LastPaidDate]   DATE          NULL,
    [RecurringType]  NVARCHAR(20)  NULL,
    [Notes]          NVARCHAR(500) NULL,
    [CreatedDate]    DATETIME      NULL,
    [ModifiedDate]   DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpensePaymentHistory')
CREATE TABLE [ExpensePaymentHistory] (
    [PaymentID]      INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ExpenseID]      INT           NULL,
    [PaymentAmount]  DECIMAL(18,2) NOT NULL,
    [PaymentDate]    DATE          NOT NULL,
    [PaymentMethod]  NVARCHAR(50)  NULL,
    [Notes]          NVARCHAR(500) NULL,
    [CreatedDate]    DATETIME      NULL
);
GO

-- Accounting tables
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanComptable')
CREATE TABLE [PlanComptable] (
    [CodeCompte]   NVARCHAR(10)  NOT NULL PRIMARY KEY,
    [Libelle]      NVARCHAR(200) NOT NULL,
    [Classe]       INT           NOT NULL,
    [TypeCompte]   NVARCHAR(20)  NOT NULL,
    [SensNormal]   NVARCHAR(10)  NOT NULL,
    [EstActif]     BIT           NULL,
    [DateCreation] DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExerciceComptable')
CREATE TABLE [ExerciceComptable] (
    [ExerciceID]    INT      IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Annee]         INT      NOT NULL,
    [DateDebut]     DATE     NOT NULL,
    [DateFin]       DATE     NOT NULL,
    [EstCloture]    BIT      NULL,
    [DateCloture]   DATETIME NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JournalComptable')
CREATE TABLE [JournalComptable] (
    [JournalID]       INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [NumPiece]        NVARCHAR(50)  NOT NULL,
    [DateEcriture]    DATETIME      NOT NULL,
    [Libelle]         NVARCHAR(500) NOT NULL,
    [TypeOperation]   NVARCHAR(50)  NULL,
    [RefExterne]      NVARCHAR(100) NULL,
    [EstValide]       BIT           NULL,
    [DateValidation]  DATETIME      NULL,
    [ValidePar]       NVARCHAR(100) NULL,
    [Remarques]       NVARCHAR(MAX) NULL,
    [DateCreation]    DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EcrituresComptables')
CREATE TABLE [EcrituresComptables] (
    [EcritureID]   INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [JournalID]    INT           NOT NULL,
    [CodeCompte]   NVARCHAR(10)  NOT NULL,
    [Libelle]      NVARCHAR(500) NOT NULL,
    [Debit]        DECIMAL(18,2) NULL,
    [Credit]       DECIMAL(18,2) NULL,
    [DateEcriture] DATETIME      NOT NULL
);
GO

-- EF Core AuditLogs
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
CREATE TABLE [AuditLogs] (
    [Id]          INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserName]    NVARCHAR(100) NOT NULL,
    [HttpMethod]  NVARCHAR(10)  NOT NULL,
    [Endpoint]    NVARCHAR(500) NOT NULL,
    [StatusCode]  INT           NOT NULL,
    [CreatedAt]   DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ── VIEWS ────────────────────────────────────────────────────────────────────

IF OBJECT_ID('V_BalanceVerification', 'V') IS NOT NULL DROP VIEW V_BalanceVerification;
GO
CREATE VIEW V_BalanceVerification AS
SELECT pc.CodeCompte, pc.Libelle, pc.Classe,
    ISNULL(SUM(ec.Debit), 0) as TotalDebit,
    ISNULL(SUM(ec.Credit), 0) as TotalCredit,
    CASE WHEN pc.SensNormal = 'Debit' THEN ISNULL(SUM(ec.Debit), 0) - ISNULL(SUM(ec.Credit), 0)
         ELSE ISNULL(SUM(ec.Credit), 0) - ISNULL(SUM(ec.Debit), 0) END as Solde
FROM PlanComptable pc
LEFT JOIN EcrituresComptables ec ON pc.CodeCompte = ec.CodeCompte
LEFT JOIN JournalComptable j ON ec.JournalID = j.JournalID
WHERE (j.EstValide = 1 OR j.EstValide IS NULL) AND pc.EstActif = 1
GROUP BY pc.CodeCompte, pc.Libelle, pc.Classe, pc.SensNormal;
GO

IF OBJECT_ID('V_GrandLivre', 'V') IS NOT NULL DROP VIEW V_GrandLivre;
GO
CREATE VIEW V_GrandLivre AS
SELECT ec.CodeCompte, pc.Libelle as LibelleCompte, j.DateEcriture, j.NumPiece,
    ec.Libelle as LibelleEcriture, ec.Debit, ec.Credit,
    SUM(ec.Debit - ec.Credit) OVER (PARTITION BY ec.CodeCompte ORDER BY j.DateEcriture, ec.EcritureID) as SoldeCumulatif,
    j.TypeOperation
FROM EcrituresComptables ec
INNER JOIN JournalComptable j ON ec.JournalID = j.JournalID
INNER JOIN PlanComptable pc ON ec.CodeCompte = pc.CodeCompte
WHERE j.EstValide = 1;
GO

IF OBJECT_ID('V_JournalGeneral', 'V') IS NOT NULL DROP VIEW V_JournalGeneral;
GO
CREATE VIEW V_JournalGeneral AS
SELECT j.JournalID, j.NumPiece, j.DateEcriture, j.Libelle as LibelleJournal,
    j.TypeOperation, j.RefExterne,
    ec.EcritureID, ec.CodeCompte, pc.Libelle as LibelleCompte,
    ec.Libelle as LibelleEcriture, ec.Debit, ec.Credit, j.EstValide
FROM JournalComptable j
INNER JOIN EcrituresComptables ec ON j.JournalID = ec.JournalID
INNER JOIN PlanComptable pc ON ec.CodeCompte = pc.CodeCompte;
GO

IF OBJECT_ID('V_SoldesComptes', 'V') IS NOT NULL DROP VIEW V_SoldesComptes;
GO
CREATE VIEW V_SoldesComptes AS
SELECT pc.CodeCompte, pc.Libelle, pc.Classe, pc.TypeCompte,
    ISNULL(SUM(ec.Debit), 0) as TotalDebit,
    ISNULL(SUM(ec.Credit), 0) as TotalCredit,
    CASE WHEN pc.SensNormal = 'Debit' THEN ISNULL(SUM(ec.Debit), 0) - ISNULL(SUM(ec.Credit), 0)
         ELSE ISNULL(SUM(ec.Credit), 0) - ISNULL(SUM(ec.Debit), 0) END as Solde,
    pc.SensNormal
FROM PlanComptable pc
LEFT JOIN EcrituresComptables ec ON pc.CodeCompte = ec.CodeCompte
LEFT JOIN JournalComptable j ON ec.JournalID = j.JournalID
WHERE pc.EstActif = 1 AND (j.EstValide = 1 OR j.EstValide IS NULL)
GROUP BY pc.CodeCompte, pc.Libelle, pc.Classe, pc.TypeCompte, pc.SensNormal;
GO

-- ── SEED DATA ─────────────────────────────────────────────────────────────────

-- Admin role — permissions set per client's purchased pages ({{ROLE_PERMISSION_VALUES}} is replaced at installer generation time)
IF NOT EXISTS (SELECT 1 FROM [Role] WHERE RoleName = 'Admin')
INSERT INTO [Role] (RoleName, Etat,
    CreateClient, ModifyClient, DeleteClient, ViewOperationClient, PayeClient, ViewClient,
    CreateFournisseur, ModifyFournisseur, DeleteFournisseur, ViewOperationFournisseur, PayeFournisseur, ViewFournisseur,
    ReverseOperation, ReverseMouvment, ViewOperation, ViewMouvment,
    ViewProjectManagment, ViewSettings, ViewUsers, EditUsers, DeleteUsers, AddUsers,
    ViewRoles, AddRoles, DeleteRoles,
    ViewFamilly, EditFamilly, DeleteFamilly, AddFamilly,
    AddArticle, DeleteArticle, EditArticle, ViewArticle,
    Repport, Ticket, SolderFournisseur, SolderClient,
    ViewFactureSettings, ModifyFactureSettings, ViewFacture,
    ViewPaymentMethod, AddPaymentMethod, ModifyPaymentMethod, DeletePaymentMethod,
    ViewApropos, Logout, ViewExit, ViewShutDown,
    ViewClientsPage, ViewFournisseurPage, ViewInventrory, ViewVente,
    CashClient, ViewCreditClient, ViewCreditFournisseur, CashFournisseur,
    AccessFacturation, CreateFacture, HistoriqueFacture, HistoryCheck, FactureEnregistrees,
    AccessLivraison, CreationLivraison, GestionLivreur)
VALUES ('Admin', 1, {{ROLE_PERMISSION_VALUES}});
GO

-- Default admin user: login = "admin", PIN = "1234"
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE UserName = 'admin')
BEGIN
    DECLARE @RoleId INT = (SELECT TOP 1 RoleID FROM [Role] WHERE RoleName = 'Admin');
    INSERT INTO [Users] (UserName, Code, RoleID, Etat) VALUES ('admin', '1234', @RoleId, 1);
END
GO

-- Default payment method
IF NOT EXISTS (SELECT 1 FROM [PaymentMethod])
    INSERT INTO [PaymentMethod] (PaymentMethodName, Etat) VALUES (N'Espèces', 1);
GO
