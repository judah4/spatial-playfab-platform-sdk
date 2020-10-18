using System;
using System.Threading.Tasks;

namespace PlatformConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var service = new ServiceAccountService();
            Task.Run(() => service.Main()).Wait();
        }
    }
}
