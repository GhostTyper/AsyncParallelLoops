using SharpFast.Async;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Development
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<int> list = new List<int>();

            for (int i = 0; i < 100; i++)
                list.Add(i);

            await ParallelLoops.ForEach<int, SqlConnection>(list, async (number, holder) => {
                Console.WriteLine($" * {number}");

                if (number == 16)
                    throw new ArgumentException("16 is not allowed.");

                await Task.Delay(1000);
            }, exception: async (number, holder, exception) => {
                Console.WriteLine($" => Oh nein, ein Fehler bei {number}: {exception.Message}");
            }, init: async () => {
                SqlConnection connection = new SqlConnection("string");

                await connection.OpenAsync();

                return connection;
            }, finalize: async (sqlConnection) => {
                await sqlConnection.DisposeAsync();
            }, threads: 7);
        }
    }
}
