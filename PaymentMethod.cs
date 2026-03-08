using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    // NO API ENDPOINT EXISTS for PaymentMethod in ZenixApi.
    // This class retains its original direct-SQL implementation until a
    // /api/payment-methods endpoint is added to the API.

    public class PaymentMethod
    {
        public int PaymentMethodID { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;

        private static readonly string ConnectionString =
            "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";

        // GET ALL
        public async Task<List<PaymentMethod>> GetPaymentMethodsAsync()
        {
            var methods = new List<PaymentMethod>();
            string query = "SELECT * FROM PaymentMethod";

            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(query, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        methods.Add(new PaymentMethod
                        {
                            PaymentMethodID = Convert.ToInt32(reader["PaymentMethodID"]),
                            PaymentMethodName = reader["PaymentMethodName"].ToString(),
                            ImagePath = reader["ImagePath"] != null
                                                ? reader["ImagePath"].ToString()
                                                : string.Empty
                        });
                    }
                }
            }
            return methods;
        }

        // INSERT
        public async Task<int> InsertPaymentMethodAsync()
        {
            string query = "INSERT INTO PaymentMethod (PaymentMethodName, ImagePath) " +
                           "VALUES (@PaymentMethodName, @ImagePath); SELECT SCOPE_IDENTITY();";

            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                try
                {
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@PaymentMethodName",
                            (object)this.PaymentMethodName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ImagePath",
                            (object)this.ImagePath ?? DBNull.Value);
                        object result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show("Payment method not inserted, error: " + err);
                    return 0;
                }
            }
        }

        // UPDATE
        public async Task<int> UpdatePaymentMethodAsync()
        {
            string query = "UPDATE PaymentMethod " +
                           "SET PaymentMethodName=@PaymentMethodName, ImagePath=@ImagePath " +
                           "WHERE PaymentMethodID=@PaymentMethodID";

            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("@PaymentMethodName",
                            (object)this.PaymentMethodName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ImagePath",
                            (object)this.ImagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PaymentMethodID", this.PaymentMethodID);
                        await cmd.ExecuteNonQueryAsync();
                        return 1;
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show("Payment method not updated: " + err);
                        return 0;
                    }
                }
            }
        }

        // DELETE
        public async Task<int> DeletePaymentMethodAsync()
        {
            string query = "DELETE FROM PaymentMethod WHERE PaymentMethodID=@PaymentMethodID";

            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("@PaymentMethodID", this.PaymentMethodID);
                        await cmd.ExecuteNonQueryAsync();
                        return 1;
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show("Payment method not deleted: " + err);
                        return 0;
                    }
                }
            }
        }
    }
}