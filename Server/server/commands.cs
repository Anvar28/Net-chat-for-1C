using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace server
{

    public enum commands
    {
        sendString = 1,
        sendFile = 2
    }

    enum TStatusSocket
    {
        none,
        receiveStream
    }

}
