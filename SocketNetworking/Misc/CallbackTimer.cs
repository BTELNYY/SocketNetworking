﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketNetworking.Misc
{
    public class CallbackTimer<T>
    {
        private Action<T> _callback;

        private float _delay;

        private T _data;

        private Task _task;

        private CancellationTokenSource _token;

        private Predicate<T> _predicate;

        private Func<T, bool> _checkFunc;

        public CallbackTimer(Action<T> action, T data, float secondDelay)
        {
            _callback = action;
            _delay = secondDelay;
            _data = data;
        }

        public CallbackTimer(Action<T> callback, T data, float secondDelay, Predicate<T> predicate) : this(callback, data, secondDelay)
        {
            _predicate = predicate;
        }

        public CallbackTimer(Action<T> callback, T data, float secondDelay, Predicate<T> predicate, Func<T, bool> checkFunc) : this(callback, data, secondDelay, predicate)
        {
            _checkFunc = checkFunc;
        }

        public CallbackTimer(Action<T> callback, T data, float secondDelay, Func<T, bool> checkFunc) : this(callback, data, secondDelay)
        {
            _checkFunc = checkFunc;
        }

        ~CallbackTimer() 
        {
            _task = null;
            _callback = null;
            _data = default(T);
        }

        public void Start()
        {
            if(_task != null)
            {
                return;
            }
            _token = new CancellationTokenSource(); 
            _token.Token.Register(Cleanup);
            _task = Task.Run(() => 
            {
                TimeSpan span = TimeSpan.FromSeconds(_delay);
                DateTime expires = DateTime.Now + span;
                while(expires > DateTime.Now)
                {
                    if(_predicate != null && !_predicate(_data))
                    {
                        _token.Cancel();
                    }
                    if(_checkFunc != null && !_checkFunc(_data))
                    {
                        _token.Cancel();
                    }
                    _token.Token.ThrowIfCancellationRequested();
                }
                _callback.Invoke(_data);
            }, _token.Token);
        }

        void Cleanup()
        {
            _task = null;
            _data = default;
            _callback = null;
            _token = null;
        }

        public void Abort()
        {
            if(_task == null)
            {
                return;
            }
            _token.Cancel();
        }
    }
}
