using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class constants
    {
        public static string separator = "#";
    }    

    public enum commands
    {
        sendString = 1 ,
        sendFile = 2
    }

}
