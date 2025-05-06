using System;

namespace SocketNetworking.Misc
{
    /// <summary>
    /// The <see cref="ChoiceEvent"/> class is intended to extend the <see cref="EventArgs"/> functionality and allow methods to determine the result of the event.
    /// </summary>
    public class ChoiceEvent : EventArgs
    {
        public ChoiceEvent()
        {

        }

        public ChoiceEvent(bool defaultState)
        {
            Accepted = defaultState;
        }

        /// <summary>
        /// Prevents the <see cref="ChoiceEvent"/> from being modified with <see cref="Accept()"/>, <see cref="Reject()"/> or <see cref="Accepted"/>.
        /// </summary>
        public void Lock()
        {
            _final = true;
        }

        /// <summary>
        /// Sets the <see cref="ChoiceEvent"/> to be rejected.
        /// </summary>
        public void Reject()
        {
            Accepted = false;
        }

        /// <summary>
        /// Sets the <see cref="ChoiceEvent"/> to be rejected with a <paramref name="message"/>.
        /// </summary>
        /// <param name="message"></param>
        public void Reject(string message)
        {
            _message = message;
            Reject();
        }

        /// <summary>
        /// Sets the <see cref="ChoiceEvent"/> to be accepted.
        /// </summary>
        public void Accept()
        {
            Accepted = true;
        }

        /// <summary>
        /// Sets the <see cref="ChoiceEvent"/> to be accepted with a <paramref name="message"/>.
        /// </summary>
        /// <param name="message"></param>
        public void Accept(string message)
        {
            _message = message;
            Accept();
        }

        string _message = string.Empty;

        /// <summary>
        /// Message (if any) set by <see cref="Accept(string)"/> or <see cref="Reject(string)"/>. Default value is <see cref="string.Empty"/>.
        /// </summary>
        public string Message => _message;

        bool _final = false;

        /// <summary>
        /// Determines if the <see cref="ChoiceEvent"/> is locked from further modification via <see cref="Accept()"/>, <see cref="Reject()"/> or <see cref="Accepted"/>
        /// </summary>
        public bool Locked
        {
            get
            {
                return _final;
            }
        }

        bool _innerState;

        /// <summary>
        /// Determines if the event has been accepted or not.
        /// </summary>
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
