using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace GestionComerce
{
    /// <summary>
    /// Improved machine locking system with hardware binding
    /// </summary>
    public static class MachineLock
    {
        private const string REGISTRY_PATH = @"SOFTWARE\GestionComerce";
        private const string REGISTRY_KEY = "InstallKey";
        private const string ENTROPY_SALT = "GC_2025_HW_LOCK_v2_SECURE";

        // Generate a unique hardware fingerprint that's hard to spoof
        private static string GetHardwareFingerprint()
        {
            try
            {
                StringBuilder fingerprint = new StringBuilder();

                // CPU ID - very hard to change
                string cpuId = GetCpuId();
                fingerprint.Append(cpuId);

                // Motherboard serial - hardware specific
                string motherboardSerial = GetMotherboardSerial();
                fingerprint.Append(motherboardSerial);

                // BIOS serial - hardware specific
                string biosSerial = GetBiosSerial();
                fingerprint.Append(biosSerial);

                // First physical disk serial - usually stays the same
                string diskSerial = GetFirstPhysicalDiskSerial();
                fingerprint.Append(diskSerial);

                // Windows Product ID - OS installation specific
                string windowsId = GetWindowsProductId();
                fingerprint.Append(windowsId);

                // Hash the combined fingerprint
                using (SHA512 sha = SHA512.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(fingerprint.ToString()));
                    return Convert.ToBase64String(hash);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to generate hardware fingerprint: " + ex.Message);
            }
        }

        private static string GetCpuId()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object processorId = obj["ProcessorId"];
                        if (processorId != null)
                            return processorId.ToString();
                    }
                }
            }
            catch { }
            return "CPU_UNKNOWN";
        }

        private static string GetMotherboardSerial()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object serial = obj["SerialNumber"];
                        if (serial != null && !string.IsNullOrWhiteSpace(serial.ToString()))
                            return serial.ToString();
                    }
                }
            }
            catch { }
            return "MOBO_UNKNOWN";
        }

        private static string GetBiosSerial()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object serial = obj["SerialNumber"];
                        if (serial != null && !string.IsNullOrWhiteSpace(serial.ToString()))
                            return serial.ToString();
                    }
                }
            }
            catch { }
            return "BIOS_UNKNOWN";
        }

        private static string GetFirstPhysicalDiskSerial()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE MediaType='Fixed hard disk media'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object serial = obj["SerialNumber"];
                        if (serial != null && !string.IsNullOrWhiteSpace(serial.ToString()))
                            return serial.ToString().Trim();
                    }
                }
            }
            catch { }
            return "DISK_UNKNOWN";
        }

        private static string GetWindowsProductId()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object productId = key.GetValue("ProductId");
                        if (productId != null)
                            return productId.ToString();
                    }
                }
            }
            catch { }
            return "WIN_UNKNOWN";
        }

        // Encrypt data with machine-specific entropy
        private static byte[] EncryptData(string data)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] entropy = Encoding.UTF8.GetBytes(ENTROPY_SALT + GetCpuId()); // Machine-specific entropy

                return ProtectedData.Protect(
                    dataBytes,
                    entropy,
                    DataProtectionScope.LocalMachine
                );
            }
            catch (Exception ex)
            {
                throw new Exception("Encryption failed: " + ex.Message);
            }
        }

        // Decrypt data with machine-specific entropy
        private static string DecryptData(byte[] encryptedData)
        {
            try
            {
                byte[] entropy = Encoding.UTF8.GetBytes(ENTROPY_SALT + GetCpuId()); // Machine-specific entropy

                byte[] decrypted = ProtectedData.Unprotect(
                    encryptedData,
                    entropy,
                    DataProtectionScope.LocalMachine
                );

                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                throw new Exception("Decryption failed: " + ex.Message);
            }
        }

        public static bool RegisterInstallation()
        {
            try
            {
                string fingerprint = GetHardwareFingerprint();

                // Add timestamp and additional validation data
                string installData = $"{fingerprint}|{DateTime.UtcNow:O}|{Guid.NewGuid()}";

                byte[] encrypted = EncryptData(installData);

                // Save to registry (primary storage)
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(REGISTRY_PATH))
                    {
                        if (key != null)
                        {
                            key.SetValue(REGISTRY_KEY, encrypted, RegistryValueKind.Binary);

                            // Add a checksum for integrity verification
                            string checksum = ComputeChecksum(encrypted);
                            key.SetValue(REGISTRY_KEY + "_CS", checksum, RegistryValueKind.String);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    throw new Exception("Administrator rights required for installation registration.");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to register installation: " + ex.Message);
            }
        }

        public static bool ValidateInstallation()
        {
            try
            {
                byte[] storedEncrypted = null;
                string storedChecksum = null;

                // Read from registry
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGISTRY_PATH))
                    {
                        if (key != null)
                        {
                            storedEncrypted = key.GetValue(REGISTRY_KEY) as byte[];
                            storedChecksum = key.GetValue(REGISTRY_KEY + "_CS") as string;
                        }
                    }
                }
                catch
                {
                    return false;
                }

                // If no registration found
                if (storedEncrypted == null || storedChecksum == null)
                {
                    return false;
                }

                // Verify integrity (detect tampering)
                string currentChecksum = ComputeChecksum(storedEncrypted);
                if (currentChecksum != storedChecksum)
                {
                    return false; // Data was tampered with
                }

                // Decrypt and verify
                string decryptedData = DecryptData(storedEncrypted);
                string[] parts = decryptedData.Split('|');

                if (parts.Length < 2)
                {
                    return false;
                }

                string storedFingerprint = parts[0];
                string currentFingerprint = GetHardwareFingerprint();

                // Match fingerprints
                return storedFingerprint == currentFingerprint;
            }
            catch
            {
                return false;
            }
        }

        // Compute checksum for integrity verification
        private static string ComputeChecksum(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }

        // Optional: Get installation date for reporting
        public static DateTime? GetInstallationDate()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        byte[] encrypted = key.GetValue(REGISTRY_KEY) as byte[];
                        if (encrypted != null)
                        {
                            string data = DecryptData(encrypted);
                            string[] parts = data.Split('|');
                            if (parts.Length >= 2)
                            {
                                return DateTime.Parse(parts[1]);
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}