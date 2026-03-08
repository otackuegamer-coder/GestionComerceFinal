namespace GestionComerce.Main
{
    /// <summary>
    /// Interface for pages that host operation and movement displays
    /// Implemented by CMainP and CMainR to allow CSingleOperation and CSingleMouvment to work with both
    /// </summary>
    public interface IOperationHost
    {
        /// <summary>
        /// Current user
        /// </summary>
        User u { get; }

        /// <summary>
        /// Reference to the main window
        /// </summary>
        MainWindow main { get; }
    }
}
