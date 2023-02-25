using System;

namespace mkpsxisoUI.Services
{
    public class ActivityLogger
    {
        private readonly Action<string> _reciever;

        public ActivityLogger(Action<string> reciever) => _reciever = reciever;

        public void LogLine(string line) => _reciever($"{line}\n");
    }
}
