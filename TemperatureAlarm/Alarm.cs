using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TemperatureAlarm {
    public class Alarm {
        private string easylogURL = @"https://apiwww.easylogcloud.com";
        private string clickatellURL = @"https://platform.clickatell.com";
        private string easylogcloudKey = ConfigurationManager.AppSettings["EasylogcloudKey"];
        private string clickatellKey = ConfigurationManager.AppSettings["ClickatellKey"];
        private string clientEmail = ConfigurationManager.AppSettings["ClientEmail"];
        private string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
        private string clientPhone = ConfigurationManager.AppSettings["ClientPhone"];
        private string clickatellPhone = ConfigurationManager.AppSettings["ClickatellPhone"];
        private string userGUID;
        private Device[] devices = new Device[int.Parse(ConfigurationManager.AppSettings["DeviceCount"])];

        private readonly Timer timer;

        public Alarm() {
            string url = easylogURL + "/Users.svc/Login" + "?APIToken=" + easylogcloudKey + "&email=" + clientEmail + "&password=" + clientSecret;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            JObject guidJSON;
            using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                guidJSON = JObject.Parse(reader.ReadToEnd());               
            }

            userGUID = guidJSON.GetValue("GUID").ToObject<string>();

            url = easylogURL + "/Devices.svc/AllDevicesSummary" + "?APIToken=" + easylogcloudKey + "&userGUID=" + userGUID + "&selectedUserGUID=" + userGUID + "&includeArchived=false";
            request = (HttpWebRequest)WebRequest.Create(url);
            response = (HttpWebResponse)request.GetResponse();

            JArray devicesJSON;
            using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                string s = reader.ReadToEnd();
                devicesJSON = JArray.Parse(s);
            }

            for (int i = 0; i < devicesJSON.Count; i++) {
                JObject j = devicesJSON[i].ToObject<JObject>();

                string deviceID = j.GetValue("GUID").ToObject<string>();
                string deviceName = j.GetValue("name").ToObject<string>();
                devices[i] = new Device(deviceID, deviceName);
            }

            timer = new Timer(30000) { AutoReset = true };
            timer.Elapsed += CheckAlarms;
        }

        private void CheckAlarms(object sender, ElapsedEventArgs e) {
            string url = easylogURL + "/Users.svc/Alarms" + "?APIToken=" + easylogcloudKey + "&userGUID=" + userGUID;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            int alarms;
            using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                alarms = int.Parse(reader.ReadToEnd());
            }

            if (alarms > 0) {
                url = easylogURL + "/Users.svc/DevicesInAlarm" + "?APIToken=" + easylogcloudKey + "&userGUID=" + userGUID + "&localTime=false";
                request = (HttpWebRequest)WebRequest.Create(url);
                response = (HttpWebResponse)request.GetResponse();

                string[] inAlarm = new string[devices.Length];
                JArray jAlarm;
                using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                    jAlarm = JArray.Parse(reader.ReadToEnd());
                }

                for (int i = 0; i < jAlarm.Count && i < inAlarm.Length; i++) {
                    inAlarm[i] = jAlarm[i].ToObject<JObject>().GetValue("name").ToObject<string>();
                }

                sendAlarmMessage(inAlarm);
            } else {
                for (int i = 0; i < devices.Length; i++) {
                    devices[i].setSendMessage(true);
                }
            }
        }

        private void sendAlarmMessage(string[] inAlarm) {
            int count = 0;
            for (int i = 0; i < devices.Length; i++) {
                bool match = false;
                for (int j = 0; j < devices.Length; j++) {
                    if (inAlarm[j] == devices[i].getName()) {
                        if (!devices[i].sendMessage()) {
                            inAlarm[j] = null;
                        } else {
                            count++;
                        }
                        match = true;
                    }
                }
                devices[i].setSendMessage(!match);
            }

            bool found = false;
            String message = "The+following+thermometers+are+in+alarm%3A";
            if (count > 0) {
                //Serial.println("The following thermometers are in alarm:");
                for (int i = 0; i < devices.Length; i++) {
                    if (inAlarm[i] != null && !inAlarm[i].Equals("")) {
                        message += "%0A";
                        message += inAlarm[i];
                        found = true;
                    }
                }
            }

            if (found) {
                string url = clickatellURL + "/messages/http/send" + "?apiKey=" + clickatellKey + "&to=" + clientPhone + "&from=" + clickatellPhone + "&content=" + message;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                    string s = reader.ReadToEnd();
                }
            }
        }

        public void Start() {
            timer.Start();
        }

        public void Stop() {
            timer.Stop();
        }
    }
}
