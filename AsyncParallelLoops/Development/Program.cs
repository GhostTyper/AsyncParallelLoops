using SharpFast.Async;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Development
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await ParallelLoops.ForEach<int, SqlConnection>(Enumerable.Range(0, 100), async (number, holder) => {
                Console.WriteLine($" * {number}");

                if (number == 16)
                    throw new ArgumentException("16 is not allowed.");

                await Task.Delay(1000);
            }, exception: async (number, holder, exception) => {
                Console.WriteLine($" => Oh nein, an error at {number}: {exception.Message}");
            }, init: async () => {
                return null;
            }, finalize: async (sqlConnection) => {
            }, threads: 7);
        }
    }
}
