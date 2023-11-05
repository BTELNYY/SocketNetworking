﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public class CallbackTimer<T>
    {
        private Action<T> _callback;

        private float _delay;

        private T _data;

        private Thread _thread;

        public CallbackTimer(Action<T> action, T data, float secondDelay)
        {
            _callback = action;
            _delay = secondDelay;
            _data = data;
        }

        public void Start()
        {
            if(_thread != null)
            {
                return;
            }
            Thread thread = new Thread(Timer);
            _thread = thread;
            thread.Start();
        }

        public void Abort()
        {
            if(_thread == null)
            {
                return;
            }
            _thread.Abort();
        }

        void Timer()
        {
            Thread.Sleep(TimeSpan.FromSeconds(_delay));
            _callback.Invoke(_data);
            _thread = null;
            _data = default;
            _delay = 0f;
            _callback = null;
        }
    }
}
