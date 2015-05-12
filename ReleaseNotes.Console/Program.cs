using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReleaseNotes.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var releaseNoteGenerator = new ReleaseNoteGenerator();
                releaseNoteGenerator.GenerateDocuments();
            }


            catch (Exception exception)
            {
                //Log
            }
        }
    }
}
