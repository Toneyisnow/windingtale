using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTools
{
    class Program
    {
        static void Main(string[] args)
        {
            LocalizationStringGenerator generator = new LocalizationStringGenerator();
            generator.Generate();


            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
