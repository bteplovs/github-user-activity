using System;

namespace gitActivity
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("One argument required: dotnet run -- <username> ");
                return;
            }

            var username = args[0];

            


        }
    }
}
