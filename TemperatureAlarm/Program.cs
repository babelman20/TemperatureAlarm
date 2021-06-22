using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace TemperatureAlarm {
    class Program {
        static void Main(string[] args) {
            var exitCode = HostFactory.Run(x => {
                x.Service<Alarm>(s => {
                    s.ConstructUsing(renamer => new Alarm());
                    s.WhenStarted(renamer => renamer.Start());
                    s.WhenStopped(renamer => renamer.Stop());
                });

                x.RunAsLocalSystem();

                x.SetServiceName("TemperatureAlarmService");
                x.SetDisplayName("Temperature Alarm Service");
                x.SetDescription("This is a service which sends a text message to the specified user when the probe temperatures go into alarm.");
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }
    }
}