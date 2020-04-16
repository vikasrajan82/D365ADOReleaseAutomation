using D365.Xrm.CICD.SolutionCustomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            D365SolutionImport smb = new D365SolutionImport("<<connstring>>", true);
            smb.ProcessSolutionImport("");
        }
    }
}
