﻿using System;

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

        public void Reject(string message)
        {
            _message = message;
            Reject();
        }

        public void Accept()
        {
            Accepted = true;
        }

        public void Accept(string message)
        {
            _message = message;
            Accept();
        }

        string _message = "";

        public string Message => _message;

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
                if (!_final)
                {
                    _innerState = value;
                }
            }
        }
    }
}
