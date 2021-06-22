using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemperatureAlarm {
    class Device {
        private string GUID;
        private string NAME;
        private bool sendMsg;

        public Device(string guid, string name) {
            this.GUID = guid;
            this.NAME = name;
            this.sendMsg = true;
        }

        public string getGUID() {
            return GUID;
        }

        public string getName() {
            return NAME;
        }

        public bool sendMessage() {
            return sendMsg;
        }

        public void setSendMessage(bool msg) {
            this.sendMsg = msg;
        }
    }
}
