using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Misc
{
    public class ChoiceEvent : EventArgs
    {
        public ChoiceEvent()
        {

        }

        public ChoiceEvent(bool defaultState)
        {
            Accepted = defaultState;
        }

        public void Lock()
        {
            _final = true;
        }

        public void Reject()
        {
            Accepted = false;
        }

        public void Accept()
        {
            Accepted = true;
        }


        bool _final = false;

        public bool Locked
        {
            get
            {
                return _final;
            }
        }

        bool _innerState;

        public bool Accepted
        {
            get
            {
                return _innerState;
            }
            set
            {
                if(!_final)
                {
                    _innerState = value;
                }
            }
        }
    }
}
